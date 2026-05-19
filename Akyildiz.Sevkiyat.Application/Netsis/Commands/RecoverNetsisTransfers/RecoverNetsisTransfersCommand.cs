using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Akyildiz.Sevkiyat.Application.Netsis.Commands.RecoverNetsisTransfers
{
    /// <summary>
    /// Hatalı Netsis kontrolü nedeniyle irsaliyesi silinmiş ve durumu geri alınmış
    /// sevkiyatları kurtarır.
    ///
    /// Hedef: AssignedToVehicle / Dispatched / ReturnedToWarehouse durumunda olup
    /// NetsisTransferredAt == null olan sevkiyatlar. ISS sipariş numaraları Netsis'te
    /// hâlâ varsa → NetsisTransferredAt geri yüklenir, durum ReadyForDispatch'e çekilir.
    /// Admin araç atamalarını yeniden yapabilir.
    /// </summary>
    public record RecoverNetsisTransfersCommand : IRequest<RecoverNetsisTransfersResult>;

    public class RecoverNetsisTransfersResult
    {
        public int Checked   { get; set; }
        public int Recovered { get; set; }
        public int NotFound  { get; set; }
        public string? Error { get; set; }
    }

    public class RecoverNetsisTransfersCommandHandler
        : IRequestHandler<RecoverNetsisTransfersCommand, RecoverNetsisTransfersResult>
    {
        private static readonly ShipmentStatus[] TargetStatuses =
        {
            ShipmentStatus.AssignedToVehicle,
            ShipmentStatus.Dispatched,
            ShipmentStatus.ReturnedToWarehouse,
        };

        private readonly IApplicationDbContext _context;
        private readonly INetsisClient _netsis;
        private readonly ILogger<RecoverNetsisTransfersCommandHandler> _logger;

        public RecoverNetsisTransfersCommandHandler(
            IApplicationDbContext context,
            INetsisClient netsis,
            ILogger<RecoverNetsisTransfersCommandHandler> logger)
        {
            _context = context;
            _netsis  = netsis;
            _logger  = logger;
        }

        public async Task<RecoverNetsisTransfersResult> Handle(
            RecoverNetsisTransfersCommand request,
            CancellationToken cancellationToken)
        {
            // Etkilenen sevkiyatlar: ileri durumlarda ama Netsis verisi yok
            // NetsisTransferredAt is the authoritative flag. IssOrder.IsTransferred can be out
            // of sync (set true by another path) while the shipment still has no transfer timestamp —
            // that is exactly the broken state this command is meant to fix.
            var candidates = await _context.Shipments
                .Include(s => s.IssOrder)
                .Where(s => TargetStatuses.Contains(s.Status)
                         && !s.NetsisTransferredAt.HasValue
                         && s.IssOrder != null
                         && s.IssOrder.ExternalOrderNumber != null)
                .ToListAsync(cancellationToken);

            if (!candidates.Any())
                return new RecoverNetsisTransfersResult();

            var orderNumbers = candidates
                .Select(s => s.IssOrder!.ExternalOrderNumber!)
                .Distinct()
                .ToList();

            _logger.LogInformation(
                "RecoverNetsisTransfers: {Count} sevkiyat kurtarma adayı, {Orders} farklı sipariş numarası Netsis'te aranıyor.",
                candidates.Count, orderNumbers.Count);

            var (existingInNetsis, netsisError) = await _netsis.CheckOrdersExistInNetsisAsync(
                orderNumbers, cancellationToken);

            if (netsisError != null)
                _logger.LogWarning("RecoverNetsisTransfers: Netsis hatası — {Error}", netsisError);

            var now = DateTime.UtcNow;
            int recovered = 0;

            foreach (var shipment in candidates)
            {
                var extNo = shipment.IssOrder!.ExternalOrderNumber!;
                if (!existingInNetsis.Contains(extNo))
                    continue;

                _logger.LogInformation(
                    "RecoverNetsisTransfers: Sevkiyat #{Id} ({Status}) kurtarılıyor — ExternalOrderNumber={No}",
                    shipment.Id, shipment.Status, extNo);

                // Netsis transfer verisini geri yükle
                shipment.MarkNetsisTransferred(now);
                shipment.IssOrder.IsTransferred        = true;
                shipment.IssOrder.NetsisOrderNumber    = extNo; // en iyi tahmin; irsaliye getir ile düzeltilebilir

                // Durumu ReadyForDispatch'e çek (admin araç atayacak)
                shipment.ChangeStatus(
                    ShipmentStatus.ReadyForDispatch,
                    null,
                    "Netsis kurtarma — sistem hatası nedeniyle geri alınan aktarım düzeltildi");

                recovered++;
            }

            if (recovered > 0)
                await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "RecoverNetsisTransfers tamamlandı: {Recovered} kurtarıldı, {NotFound} Netsis'te bulunamadı.",
                recovered, candidates.Count - recovered);

            return new RecoverNetsisTransfersResult
            {
                Checked   = candidates.Count,
                Recovered = recovered,
                NotFound  = candidates.Count - recovered,
                Error     = netsisError,
            };
        }
    }
}
