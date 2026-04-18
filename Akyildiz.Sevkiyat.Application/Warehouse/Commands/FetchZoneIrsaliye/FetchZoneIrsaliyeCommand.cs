using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.External.Netsis.Dtos;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.FetchZoneIrsaliye
{
    public record FetchZoneIrsaliyeCommand(int ZonePreparationId) : IRequest<FetchZoneIrsaliyeResult>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Warehouse", "Driver" };
    }

    public record FetchZoneIrsaliyeResult(int Fetched, int Skipped, List<string> Errors, List<string> Warnings);

    public class FetchZoneIrsaliyeCommandHandler : IRequestHandler<FetchZoneIrsaliyeCommand, FetchZoneIrsaliyeResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly INetsisClient _netsisClient;

        public FetchZoneIrsaliyeCommandHandler(IApplicationDbContext context, INetsisClient netsisClient)
        {
            _context      = context;
            _netsisClient = netsisClient;
        }

        public async Task<FetchZoneIrsaliyeResult> Handle(FetchZoneIrsaliyeCommand request, CancellationToken cancellationToken)
        {
            var zp = await _context.ZonePreparations
                .FirstOrDefaultAsync(z => z.Id == request.ZonePreparationId, cancellationToken)
                ?? throw new NotFoundException("ZonePreparation", request.ZonePreparationId);

            if (zp.Status != ZonePreparationStatus.ReadyForDriverInfo)
                throw new DomainException("İrsaliye çekimi yalnızca 'Sevke Hazır' aşamasındaki hazırlıklar için yapılabilir.");

            var shipments = await _context.Shipments
                .Include(s => s.Project)
                .Include(s => s.IssOrder)
                .Where(s =>
                    s.ZonePreparationId == zp.Id &&
                    s.Status != ShipmentStatus.Cancelled &&
                    s.Status != ShipmentStatus.Passive &&
                    s.Status != ShipmentStatus.Created)
                .ToListAsync(cancellationToken);

            int fetched = 0, skipped = 0;
            var errors   = new List<string>();
            var warnings = new List<string>();

            foreach (var shipment in shipments)
            {
                // Sipariş henüz Netsis'e aktarılmamış
                if (!shipment.NetsisTransferredAt.HasValue)
                {
                    warnings.Add($"Sevkiyat #{shipment.Id} ({shipment.Project?.Name}): Sipariş henüz Netsis'e aktarılmamış. Önce 'Netsis'e Aktar' işlemini yapın.");
                    skipped++;
                    continue;
                }

                // Zaten irsaliye numarası var → tekrar çekmeye gerek yok
                if (!string.IsNullOrWhiteSpace(shipment.IrsaliyeNo))
                {
                    skipped++;
                    continue;
                }

                var orderNo = shipment.IssOrder?.NetsisOrderNumber;
                if (string.IsNullOrWhiteSpace(orderNo))
                {
                    warnings.Add($"Sevkiyat #{shipment.Id}: Netsis sipariş numarası kaydedilmemiş.");
                    skipped++;
                    continue;
                }

                var cariKod = shipment.Project?.NetsisCariKodu;
                if (string.IsNullOrWhiteSpace(cariKod))
                {
                    errors.Add($"Sevkiyat #{shipment.Id}: Proje '{shipment.Project?.Name}' için Netsis Cari Kodu tanımlanmamış.");
                    skipped++;
                    continue;
                }

                try
                {
                    var irsaliyeler = await _netsisClient.GetIrsaliyelerAsync(
                        new NetsisIrsaliyeQuery { SiparisNo = orderNo, CariKod = cariKod },
                        cancellationToken);

                    if (irsaliyeler == null || !irsaliyeler.Any())
                    {
                        warnings.Add($"Sevkiyat #{shipment.Id} ({shipment.Project?.Name}): İrsaliye henüz kesilmemiş. (Sipariş No: {orderNo})");
                        skipped++;
                        continue;
                    }

                    // İlk irsaliyeyi kullan (genellikle tek irsaliye olur)
                    var irsaliye = irsaliyeler.First();
                    shipment.SetIrsaliyeInfo(irsaliye.IrsaliyeNo, irsaliye.IrsaliyeTarihi);
                    fetched++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Sevkiyat #{shipment.Id}: {ex.Message}");
                    skipped++;
                }
            }

            // IrsaliyeFetched → tüm aktarılmış sevkiyatların irsaliyeleri çekildiğinde true
            var transferred = shipments.Where(s => s.NetsisTransferredAt.HasValue).ToList();
            bool allHaveIrsaliye = transferred.Any()
                && transferred.All(s => !string.IsNullOrWhiteSpace(s.IrsaliyeNo));

            if (allHaveIrsaliye)
                zp.IrsaliyeFetched = true;

            await _context.SaveChangesAsync(cancellationToken);

            return new FetchZoneIrsaliyeResult(fetched, skipped, errors, warnings);
        }
    }
}
