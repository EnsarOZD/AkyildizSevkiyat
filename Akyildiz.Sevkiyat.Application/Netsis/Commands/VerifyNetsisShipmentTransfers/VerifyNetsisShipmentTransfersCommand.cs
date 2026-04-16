using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Akyildiz.Sevkiyat.Application.Netsis.Commands.VerifyNetsisShipmentTransfers
{
    /// <summary>
    /// Netsis'e aktarılmış görünen ama Netsis tarafından silinmiş olabilecek
    /// sevkiyatları kontrol eder. Silinmişse NetsisTransferredAt sıfırlanır,
    /// böylece sevkiyat tekrar "Netsis'e Aktar" ile gönderilebilir.
    /// </summary>
    public record VerifyNetsisShipmentTransfersCommand : IRequest<VerifyNetsisShipmentTransfersResult>;

    public class VerifyNetsisShipmentTransfersResult
    {
        public int Checked       { get; set; }
        public int Reset         { get; set; }   // Netsis'te hiç bulunamayan → sıfırlanan
        public int StillOk       { get; set; }   // Netsis'te bizim aktarımımız olarak mevcut
        public int Foreign       { get; set; }   // Netsis'te var ama başka sistem aktarmış — dokunulmadı
        public int AutoDetected  { get; set; }   // Aktarılmamış ama Netsis'te bulundu → otomatik işaretlendi
        public string? Error     { get; set; }
    }

    public class VerifyNetsisShipmentTransfersCommandHandler
        : IRequestHandler<VerifyNetsisShipmentTransfersCommand, VerifyNetsisShipmentTransfersResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly INetsisClient _netsis;
        private readonly ILogger<VerifyNetsisShipmentTransfersCommandHandler> _logger;

        public VerifyNetsisShipmentTransfersCommandHandler(
            IApplicationDbContext context,
            INetsisClient netsis,
            ILogger<VerifyNetsisShipmentTransfersCommandHandler> logger)
        {
            _context = context;
            _netsis  = netsis;
            _logger  = logger;
        }

        public async Task<VerifyNetsisShipmentTransfersResult> Handle(
            VerifyNetsisShipmentTransfersCommand request,
            CancellationToken cancellationToken)
        {
            // Netsis'e aktarılmış ama henüz teslim edilmemiş sevkiyatları al.
            // Teslim edilmişler zaten işlenmiş, tekrar aktarılmaları anlamsız.
            var exportableStatuses = new[]
            {
                ShipmentStatus.ReadyForDispatch,
                ShipmentStatus.AssignedToVehicle,
            };

            var shipments = await _context.Shipments
                .Include(s => s.IssOrder)
                .Where(s => s.NetsisTransferredAt.HasValue
                         && exportableStatuses.Contains(s.Status)
                         && s.IssOrder != null
                         && s.IssOrder.NetsisOrderNumber != null)
                .Select(s => new
                {
                    s.Id,                                              // EKACK1 kontrolü için
                    NetsisOrderNumber = s.IssOrder!.NetsisOrderNumber!,
                })
                .ToListAsync(cancellationToken);

            if (!shipments.Any())
            {
                var emptyResult = new VerifyNetsisShipmentTransfersResult();
                emptyResult.AutoDetected = await DetectAlreadyInNetsisAsync(cancellationToken);
                return emptyResult;
            }

            _logger.LogInformation(
                "VerifyNetsisTransfers: {Count} aktarılmış sevkiyat kontrol ediliyor.", shipments.Count);

            var (missing, foreign, netsisError) = await _netsis.CheckShipmentOrdersMissingAsync(
                shipments.Select(s => (s.NetsisOrderNumber, s.Id.ToString())),
                cancellationToken);

            if (netsisError != null)
                _logger.LogWarning("VerifyNetsisTransfers: Netsis hatası — {Error}", netsisError);

            var result = new VerifyNetsisShipmentTransfersResult
            {
                Checked = shipments.Count,
                Foreign = foreign.Count,
                Error   = netsisError,
            };

            if (foreign.Any())
                _logger.LogWarning(
                    "VerifyNetsisTransfers: {Count} sevkiyat başka bir sistem tarafından Netsis'e aktarılmış " +
                    "(EKACK1 eşleşmedi). NetsisTransferredAt sıfırlanmadı — sipariş Netsis'te mevcut.",
                    foreign.Count);

            if (missing.Any())
            {
                // Silinmiş olanları bul ve sıfırla
                var missingIds = shipments
                    .Where(s => missing.Contains(s.NetsisOrderNumber))
                    .Select(s => s.Id)
                    .ToHashSet();

                var toReset = await _context.Shipments
                    .Include(s => s.IssOrder)
                    .Where(s => missingIds.Contains(s.Id))
                    .ToListAsync(cancellationToken);

                foreach (var shipment in toReset)
                {
                    _logger.LogWarning(
                        "VerifyNetsisTransfers: Sevkiyat #{Id} — Netsis'te bulunamadı, aktarım sıfırlanıyor. NetsisOrderNumber={No}",
                        shipment.Id, shipment.IssOrder?.NetsisOrderNumber);

                    shipment.RevertNetsisTransfer();

                    if (shipment.IssOrder != null)
                        shipment.IssOrder.NetsisOrderNumber = null;
                }

                await _context.SaveChangesAsync(cancellationToken);

                result.Reset   = toReset.Count;
                result.StillOk = shipments.Count - toReset.Count - foreign.Count;
            }
            else
            {
                result.StillOk = shipments.Count - foreign.Count;
            }

            // ── 2. Henüz aktarılmamış sevkiyatları kontrol et ────────────────────
            result.AutoDetected = await DetectAlreadyInNetsisAsync(cancellationToken);

            return result;
        }

        /// <summary>
        /// NetsisTransferredAt == null olan exportable sevkiyatların sipariş numaralarının
        /// Netsis'te mevcut olup olmadığını kontrol eder. Bulunursa MarkNetsisTransferred
        /// ile işaretler ve NetsisOrderNumber'ı doldurur.
        /// </summary>
        private async Task<int> DetectAlreadyInNetsisAsync(CancellationToken cancellationToken)
        {
            var exportableStatuses = new[]
            {
                ShipmentStatus.ReadyForDispatch,
                ShipmentStatus.AssignedToVehicle,
            };

            var candidates = await _context.Shipments
                .Include(s => s.IssOrder)
                .Where(s => !s.NetsisTransferredAt.HasValue
                         && exportableStatuses.Contains(s.Status)
                         && s.IssOrder != null
                         && s.IssOrder.ExternalOrderNumber != null
                         && s.IssOrder.NetsisOrderNumber == null)
                .Select(s => new
                {
                    s.Id,
                    ExternalOrderNumber = s.IssOrder!.ExternalOrderNumber!,
                })
                .ToListAsync(cancellationToken);

            if (!candidates.Any()) return 0;

            _logger.LogInformation(
                "VerifyNetsisTransfers: {Count} aktarılmamış sevkiyat Netsis'te aranıyor.",
                candidates.Count);

            var (found, checkError) = await _netsis.CheckOrdersExistInNetsisAsync(
                candidates.Select(c => c.ExternalOrderNumber),
                cancellationToken);

            if (checkError != null)
                _logger.LogWarning("VerifyNetsisTransfers (auto-detect): Netsis hatası — {Error}", checkError);

            if (!found.Any()) return 0;

            var foundIds = candidates
                .Where(c => found.Contains(c.ExternalOrderNumber))
                .Select(c => c.Id)
                .ToHashSet();

            var toMark = await _context.Shipments
                .Include(s => s.IssOrder)
                .Where(s => foundIds.Contains(s.Id))
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            foreach (var shipment in toMark)
            {
                _logger.LogInformation(
                    "VerifyNetsisTransfers: Sevkiyat #{Id} — Netsis'te bulundu, otomatik işaretleniyor. ExternalOrderNumber={No}",
                    shipment.Id, shipment.IssOrder?.ExternalOrderNumber);

                shipment.MarkNetsisTransferred(now);

                if (shipment.IssOrder != null)
                    shipment.IssOrder.NetsisOrderNumber = shipment.IssOrder.ExternalOrderNumber;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return toMark.Count;
        }
    }
}
