using Akyildiz.Sevkiyat.Application.External.Netsis.Dtos;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.Reconciliation.Services;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Netsis.Commands.BulkExportShipmentsToNetsis
{
    public record BulkExportShipmentsToNetsisCommand(List<int> ShipmentIds) : IRequest<BulkExportShipmentsToNetsisResult>;

    public record BulkExportShipmentsToNetsisResult(int Exported, int Skipped, List<string> Errors);

    public class BulkExportShipmentsToNetsisCommandHandler
        : IRequestHandler<BulkExportShipmentsToNetsisCommand, BulkExportShipmentsToNetsisResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly INetsisClient _netsisClient;
        private readonly ICurrentUserService _currentUserService;
        private readonly ReconciliationGuard _guard;

        public BulkExportShipmentsToNetsisCommandHandler(
            IApplicationDbContext context,
            INetsisClient netsisClient,
            ICurrentUserService currentUserService,
            ReconciliationGuard guard)
        {
            _context = context;
            _netsisClient = netsisClient;
            _currentUserService = currentUserService;
            _guard = guard;
        }

        public async Task<BulkExportShipmentsToNetsisResult> Handle(
            BulkExportShipmentsToNetsisCommand request, CancellationToken cancellationToken)
        {
            var exportableStatuses = new[] { ShipmentStatus.ReadyForDispatch, ShipmentStatus.AssignedToVehicle };

            var shipments = await _context.Shipments
                .Include(s => s.Project)
                .Include(s => s.Lines).ThenInclude(l => l.IssOrderLine)
                .Include(s => s.Lines).ThenInclude(l => l.StockMaster)
                .Include(s => s.IssOrder)
                .Where(s => request.ShipmentIds.Contains(s.Id))
                .ToListAsync(cancellationToken);

            // Tüm sevkiyatların Netsis stok kodlarını toplu çek — döngüde tekrar sorgu yapmamak için
            var allNetsisKodlari = shipments
                .SelectMany(s => s.Lines)
                .Select(l => l.StockMaster?.NetsisStockCode)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c!)
                .Distinct()
                .ToList();

            var kdvMap = await _netsisClient.GetStockKdvRatesAsync(allNetsisKodlari, cancellationToken);

            int exported = 0, skipped = 0;
            var errors = new List<string>();

            foreach (var shipment in shipments)
            {
                // Durum kontrolü
                if (!exportableStatuses.Contains(shipment.Status))
                {
                    errors.Add($"Sevkiyat #{shipment.Id}: Aktarılabilir durumda değil ({shipment.Status}).");
                    skipped++;
                    continue;
                }

                // Zaten aktarılmış
                if (shipment.NetsisTransferredAt.HasValue)
                {
                    skipped++;
                    continue;
                }

                // Cari kodu eksik
                if (string.IsNullOrWhiteSpace(shipment.Project?.NetsisCariKodu))
                {
                    errors.Add($"Sevkiyat #{shipment.Id}: Proje '{shipment.Project?.Name}' için Netsis Cari Kodu tanımlanmamış.");
                    skipped++;
                    continue;
                }

                // ISS projeleri için NetsisTeslimCariKodu zorunlu — manuel müşterilerde opsiyonel.
                if (shipment.Project!.Source == Domain.Enums.ProjectSource.Iss
                    && string.IsNullOrWhiteSpace(shipment.Project.NetsisTeslimCariKodu))
                {
                    errors.Add($"Sevkiyat #{shipment.Id}: Proje '{shipment.Project.Name}' için Netsis Teslim Cari Kodu tanımlanmamış.");
                    skipped++;
                    continue;
                }

                // Açık uzlaştırma hatası
                var hasOpenError = await _context.ReconciliationIssues
                    .AnyAsync(i => i.ShipmentId == shipment.Id
                                && i.Status == ReconciliationStatus.Open
                                && i.Severity == ReconciliationSeverity.Error, cancellationToken);
                if (hasOpenError)
                {
                    errors.Add($"Sevkiyat #{shipment.Id}: Açık uzlaştırma hatası mevcut.");
                    skipped++;
                    continue;
                }

                // Netsis stok kodu eksik
                var missingNetsisCode = shipment.Lines
                    .Where(l => l.StockMaster == null || string.IsNullOrWhiteSpace(l.StockMaster.NetsisStockCode))
                    .Select(l => l.StockName)
                    .ToList();
                if (missingNetsisCode.Any())
                {
                    errors.Add($"Sevkiyat #{shipment.Id}: Netsis stok kodu eksik: {string.Join(", ", missingNetsisCode.Take(3))}" +
                               (missingNetsisCode.Count > 3 ? $" (+{missingNetsisCode.Count - 3} daha)" : ""));
                    skipped++;
                    continue;
                }

                try
                {
                    var belgeNo = new[] { shipment.TalepNo, shipment.IssOrder?.TalepNo, shipment.IssOrder?.ExternalOrderNumber }
                        .FirstOrDefault(c => !string.IsNullOrWhiteSpace(c) && c != "0");

                    // DeliveredQty == 0 olan kalemleri filtrele
                    var linesToExport = shipment.Lines.Where(l => l.DeliveredQty > 0).ToList();
                    if (linesToExport.Count == 0)
                    {
                        errors.Add($"Sevkiyat #{shipment.Id}: Tüm kalemlerin toplanan miktarı 0. Netsis'e gönderilecek kalem yok.");
                        skipped++;
                        continue;
                    }

                    var lines = linesToExport
                        .Select((l, idx) => new NetsisSiparisLine
                        {
                            SatirNo     = idx + 1,
                            StokKodu    = l.StockMaster!.NetsisStockCode!,
                            Miktar      = l.DeliveredQty,
                            Birim       = l.Unit.ToString(),
                            BirimFiyati = l.IssOrderLine?.BirimFiyati ?? 0,
                            KdvOrani    = l.StockMaster?.NetsisStockCode != null &&
                                          kdvMap.TryGetValue(l.StockMaster.NetsisStockCode, out var nKdv)
                                              ? nKdv
                                              : (decimal)(int)(l.StockMaster?.TaxRate ?? 0),
                        }).ToList();

                    var siparisRequest = new NetsisSiparisRequest
                    {
                        BelgeNo      = belgeNo ?? string.Empty,
                        CariKodu     = shipment.Project!.NetsisCariKodu!,
                        // Manuel için: NetsisCariKodu fallback (Project.Code = MM-XXXX Netsis'te anlamsız).
                        // ISS için: NetsisTeslimCariKodu (yukarıda zorunlu) → Project.Code fallback.
                        ProjeKodu    = shipment.Project.Source == Domain.Enums.ProjectSource.Manual
                            ? (shipment.Project.NetsisTeslimCariKodu ?? shipment.Project.NetsisCariKodu ?? string.Empty)
                            : (shipment.Project.NetsisTeslimCariKodu ?? shipment.Project.Code ?? string.Empty),
                        TeslimTarihi = shipment.DeliveryDate,
                        SiparisId    = shipment.Id.ToString(),
                        KurumKodu    = shipment.Project.InstitutionCode,
                        TalepNo      = shipment.TalepNo ?? shipment.IssOrder?.TalepNo,
                        TalepTuru    = shipment.IssOrder?.TalepTuru,
                        Donem        = shipment.IssOrder?.Donem,
                        TeslimAlacakKisiler           = shipment.IssOrder?.TeslimAlacakKisiler,
                        TeslimAlacakTelefonNumaralari = shipment.IssOrder?.TeslimAlacakTelefonNumaralari,
                        YoneticiMailAdresleri         = shipment.IssOrder?.YoneticiMailAdresleri,
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

                    _context.ShipmentHistories.Add(new Domain.Entities.ShipmentHistory
                    {
                        ShipmentId      = shipment.Id,
                        OldStatus       = shipment.Status,
                        NewStatus       = shipment.Status,
                        ChangedAt       = DateTime.UtcNow,
                        ChangedByUserId = _currentUserService.UserId,
                        Description     = $"Netsis'e aktarıldı. Belge No: {shipment.IssOrder?.NetsisOrderNumber}",
                    });

                    exported++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Sevkiyat #{shipment.Id}: {ex.Message}");
                    skipped++;
                }
            }

            if (exported > 0)
                await _context.SaveChangesAsync(cancellationToken);

            return new BulkExportShipmentsToNetsisResult(exported, skipped, errors);
        }
    }
}
