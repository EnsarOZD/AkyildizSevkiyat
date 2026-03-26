using Akyildiz.Sevkiyat.Application.External.Netsis.Dtos;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.FetchZoneIrsaliye
{
    public record FetchZoneIrsaliyeCommand(int ZonePreparationId) : IRequest<FetchZoneIrsaliyeResult>;

    public record FetchZoneIrsaliyeResult(int Exported, int Skipped, List<string> Errors);

    public class FetchZoneIrsaliyeCommandHandler : IRequestHandler<FetchZoneIrsaliyeCommand, FetchZoneIrsaliyeResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly INetsisClient _netsisClient;

        public FetchZoneIrsaliyeCommandHandler(IApplicationDbContext context, INetsisClient netsisClient)
        {
            _context          = context;
            _netsisClient     = netsisClient;
        }

        public async Task<FetchZoneIrsaliyeResult> Handle(FetchZoneIrsaliyeCommand request, CancellationToken cancellationToken)
        {
            var zp = await _context.ZonePreparations
                .FirstOrDefaultAsync(z => z.Id == request.ZonePreparationId, cancellationToken)
                ?? throw new NotFoundException("ZonePreparation", request.ZonePreparationId);

            if (zp.Status != ZonePreparationStatus.ReadyForDriverInfo)
                throw new DomainException("İrsaliye çekimi yalnızca 'Sevke Hazır' aşamasındaki hazırlıklar için yapılabilir.");

            // Load shipments that need Netsis export
            var shipments = await _context.Shipments
                .Include(s => s.Project)
                .Include(s => s.Lines)
                    .ThenInclude(l => l.IssOrderLine)
                .Include(s => s.IssOrder)
                .Where(s =>
                    s.ZonePreparationId == zp.Id &&
                    s.Status != ShipmentStatus.Cancelled &&
                    s.Status != ShipmentStatus.Passive &&
                    s.Status != ShipmentStatus.Created
                )
                .ToListAsync(cancellationToken);

            int exported = 0;
            int skipped = 0;
            var errors = new List<string>();

            foreach (var shipment in shipments)
            {
                // Skip if already transferred
                if (shipment.NetsisTransferredAt.HasValue)
                {
                    skipped++;
                    continue;
                }

                // Skip if project missing cari kodu
                if (string.IsNullOrWhiteSpace(shipment.Project?.NetsisCariKodu))
                {
                    errors.Add($"Proje '{shipment.Project?.Name ?? shipment.Id.ToString()}' için Netsis Cari Kodu tanımlanmamış.");
                    skipped++;
                    continue;
                }

                // Skip if shipment has open reconciliation errors
                var hasOpenError = await _context.ReconciliationIssues
                    .AnyAsync(i => i.ShipmentId == shipment.Id
                                && i.Status == ReconciliationStatus.Open
                                && i.Severity == ReconciliationSeverity.Error, cancellationToken);
                if (hasOpenError)
                {
                    errors.Add($"Sevkiyat #{shipment.Id}: Açık uzlaştırma hatası mevcut. Netsis aktarımı atlandı. Uzlaştırma ekranından sorunu çözün.");
                    skipped++;
                    continue;
                }

                try
                {
                    var belgeNo = shipment.TalepNo
                        ?? shipment.IssOrder?.TalepNo
                        ?? shipment.IssOrder?.ExternalOrderNumber;

                    var lines = shipment.Lines
                        .Where(l => l.IssOrderLine != null)
                        .Select((l, idx) => new NetsisSiparisLine
                        {
                            SatirNo     = idx + 1,
                            StokKodu    = l.IssOrderLine!.StockCode,
                            Miktar      = l.OrderedQty,
                            Birim       = l.IssOrderLine.Unit.ToString(),
                            BirimFiyati = l.IssOrderLine.BirimFiyati,
                            KdvOrani    = l.IssOrderLine.KDVOrani,
                        })
                        .ToList();

                    var siparisRequest = new NetsisSiparisRequest
                    {
                        BelgeNo      = belgeNo ?? string.Empty,
                        CariKodu     = shipment.Project!.NetsisCariKodu!,
                        ProjeKodu    = shipment.Project.Code,
                        TeslimTarihi = shipment.DeliveryDate,
                        Satirlar     = lines,
                    };

                    var result = await _netsisClient.CreateSiparisAsync(siparisRequest, cancellationToken);

                    if (!result.Basarili)
                    {
                        errors.Add($"Sevkiyat #{shipment.Id}: {result.Mesaj ?? "Bilinmeyen Netsis hatası"}");
                        skipped++;
                        continue;
                    }

                    shipment.MarkNetsisTransferred(DateTime.UtcNow);
                    if (shipment.IssOrder != null)
                        shipment.IssOrder.NetsisOrderNumber = result.NetsisOrderNo ?? siparisRequest.BelgeNo;

                    exported++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Sevkiyat #{shipment.Id}: {ex.Message}");
                    skipped++;
                }
            }

            // IrsaliyeFetched ancak tüm işlenebilir sevkiyatlar hatasız Netsis'e aktarıldığında true olur.
            // "İşlenebilir" = daha önce aktarılmamış olanlar (NetsisTransferredAt == null).
            // Kısmi başarı (exported > 0 ama errors > 0) → false kalır; "Araca Ata" butonu açılmaz.
            int processable = shipments.Count(s => !s.NetsisTransferredAt.HasValue);
            bool allDoneWithoutError = errors.Count == 0
                && (processable == 0 || exported == processable);

            if (allDoneWithoutError)
                zp.IrsaliyeFetched = true;

            await _context.SaveChangesAsync(cancellationToken);

            return new FetchZoneIrsaliyeResult(exported, skipped, errors);
        }
    }
}
