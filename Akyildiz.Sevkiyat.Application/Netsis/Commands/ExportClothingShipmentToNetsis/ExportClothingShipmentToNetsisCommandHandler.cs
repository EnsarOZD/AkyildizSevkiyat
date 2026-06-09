using Akyildiz.Sevkiyat.Application.External.Netsis.Dtos;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Netsis.Commands.ExportClothingShipmentToNetsis
{
    public class ExportClothingShipmentToNetsisCommandHandler
        : IRequestHandler<ExportClothingShipmentToNetsisCommand, ExportClothingShipmentToNetsisResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly INetsisClient _netsisClient;
        private readonly ICurrentUserService _currentUserService;

        public ExportClothingShipmentToNetsisCommandHandler(
            IApplicationDbContext context,
            INetsisClient netsisClient,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _netsisClient = netsisClient;
            _currentUserService = currentUserService;
        }

        public async Task<ExportClothingShipmentToNetsisResult> Handle(
            ExportClothingShipmentToNetsisCommand request, CancellationToken cancellationToken)
        {
            var shipment = await _context.Shipments
                .Include(s => s.Project)
                .Include(s => s.Lines)
                    .ThenInclude(l => l.IssOrderLine)
                .Include(s => s.Lines)
                    .ThenInclude(l => l.StockMaster)
                .Include(s => s.IssOrder)
                .FirstOrDefaultAsync(s => s.Id == request.ShipmentId, cancellationToken)
                ?? throw new NotFoundException("Shipment", request.ShipmentId);

            if (shipment.OperationType != OperationType.Clothing)
                throw new DomainException(
                    "Bu endpoint yalnızca Kıyafet (Clothing) operasyonu sevkiyatları için kullanılabilir.");

            // Hazırlık atlanmışsa 'Oluşturuldu'dan; depo hazırlığı yapıldıysa 'Sevke Hazır'dan aktarılır.
            if (shipment.Status != ShipmentStatus.Created && shipment.Status != ShipmentStatus.ReadyForDispatch)
                throw new DomainException(
                    $"Kıyafet Netsis aktarımı yalnızca 'Oluşturuldu' veya 'Sevke Hazır' durumundaki sevkiyatlar için geçerlidir. " +
                    $"Mevcut durum: {shipment.Status}");

            // Hazırlık yapıldıysa irsaliyeye toplanan miktar, aksi halde sipariş miktarı yazılır.
            bool usePreparedQty = shipment.Status == ShipmentStatus.ReadyForDispatch;

            if (shipment.NetsisTransferredAt.HasValue)
                throw new Akyildiz.Sevkiyat.Domain.Exceptions.ConflictException(
                    $"Sevkiyat #{shipment.Id} zaten Netsis'e aktarılmış " +
                    $"({shipment.NetsisTransferredAt:dd.MM.yyyy HH:mm}).");

            if (string.IsNullOrWhiteSpace(shipment.Project.NetsisCariKodu))
                throw new DomainException(
                    $"Proje '{shipment.Project.Name}' için Netsis Cari Kodu tanımlanmamış.");

            // ISS projeleri için NetsisTeslimCariKodu zorunlu — manuelde opsiyonel.
            if (shipment.Project.Source == Domain.Enums.ProjectSource.Iss
                && string.IsNullOrWhiteSpace(shipment.Project.NetsisTeslimCariKodu))
                throw new DomainException(
                    $"Proje '{shipment.Project.Name}' için Netsis Teslim Cari Kodu tanımlanmamış.");

            // Aktarılacak kalemler: hazırlıkta toplanan (DeliveredQty) ya da sipariş (OrderedQty)
            // miktarı; 0 olanlar (verilemeyen kalemler) irsaliyeye dahil edilmez.
            var effectiveLines = shipment.Lines
                .Select(l => new { Line = l, Qty = usePreparedQty ? l.DeliveredQty : l.OrderedQty })
                .Where(x => x.Qty > 0)
                .ToList();

            if (effectiveLines.Count == 0)
                throw new DomainException("Aktarılacak (miktarı 0'dan büyük) kalem bulunamadı.");

            // NetsisStockCode doğrulaması (yalnızca aktarılacak kalemler)
            var missingNetsisCode = effectiveLines
                .Where(x => x.Line.StockMaster == null || string.IsNullOrWhiteSpace(x.Line.StockMaster.NetsisStockCode))
                .Select(x => x.Line.StockName)
                .ToList();

            if (missingNetsisCode.Any())
                throw new DomainException(
                    $"Aşağıdaki ürünlerin Netsis Stok Kodu tanımlanmamış: {string.Join(", ", missingNetsisCode)}");

            var belgeNo = PickBelgeNo(
                shipment.IssOrder?.ExternalOrderNumber,
                shipment.TalepNo,
                shipment.IssOrder?.TalepNo);

            var netsisKodlari = effectiveLines
                .Select(x => x.Line.StockMaster?.NetsisStockCode)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c!)
                .Distinct()
                .ToList();

            var kdvMap = await _netsisClient.GetStockKdvRatesAsync(netsisKodlari, cancellationToken);

            var lines = effectiveLines
                .Select((x, idx) => new NetsisSiparisLine
                {
                    SatirNo     = idx + 1,
                    StokKodu    = x.Line.StockMaster!.NetsisStockCode!,
                    Miktar      = x.Qty,
                    Birim       = x.Line.Unit.ToString(),
                    BirimFiyati = x.Line.IssOrderLine?.BirimFiyati ?? 0,
                    KdvOrani    = x.Line.StockMaster?.NetsisStockCode != null &&
                                  kdvMap.TryGetValue(x.Line.StockMaster.NetsisStockCode, out var nKdv)
                                      ? nKdv
                                      : (decimal)(int)(x.Line.StockMaster?.TaxRate ?? 0),
                })
                .ToList();

            var siparisRequest = new NetsisSiparisRequest
            {
                BelgeNo      = belgeNo ?? string.Empty,
                CariKodu     = shipment.Project.NetsisCariKodu!,
                // Manuel için: NetsisCariKodu fallback. ISS için: NetsisTeslimCariKodu → Project.Code.
                ProjeKodu    = shipment.Project.Source == Domain.Enums.ProjectSource.Manual
                    ? (shipment.Project.NetsisTeslimCariKodu ?? shipment.Project.NetsisCariKodu ?? string.Empty)
                    : (shipment.Project.NetsisTeslimCariKodu ?? shipment.Project.Code ?? string.Empty),
                DepoKodu     = "2",
                TeslimTarihi = shipment.DeliveryDate,
                SiparisId                     = shipment.Id.ToString(),
                KurumKodu                     = shipment.Project.InstitutionCode,
                TalepNo                       = shipment.TalepNo ?? shipment.IssOrder?.TalepNo,
                TalepTuru                     = shipment.IssOrder?.TalepTuru,
                Donem                         = shipment.IssOrder?.Donem,
                TeslimAlacakKisiler           = shipment.IssOrder?.TeslimAlacakKisiler,
                TeslimAlacakTelefonNumaralari = shipment.IssOrder?.TeslimAlacakTelefonNumaralari,
                YoneticiMailAdresleri         = shipment.IssOrder?.YoneticiMailAdresleri,
                Satirlar                      = lines,
            };

            var result = await _netsisClient.CreateSiparisAsync(siparisRequest, cancellationToken);

            if (!result.Basarili)
                throw new DomainException(
                    $"Netsis sipariş aktarımı başarısız: {result.Mesaj ?? "Bilinmeyen hata"}");

            // Başarılı — aktarım bilgilerini güncelle
            shipment.MarkNetsisTransferred(DateTime.UtcNow);
            if (shipment.IssOrder != null)
                shipment.IssOrder.NetsisOrderNumber = result.NetsisOrderNo ?? siparisRequest.BelgeNo;

            // Kıyafet: doğrudan Delivered'a taşı
            shipment.SkipToDelivered();

            // Audit kaydı
            _context.ShipmentHistories.Add(new Domain.Entities.ShipmentHistory
            {
                ShipmentId      = shipment.Id,
                OldStatus       = Domain.Enums.ShipmentStatus.Created,
                NewStatus       = Domain.Enums.ShipmentStatus.Delivered,
                ChangedAt       = DateTime.UtcNow,
                ChangedByUserId = _currentUserService.UserId,
                Description     = $"Kıyafet — Netsis'e aktarıldı ve teslim edildi. Belge No: {shipment.IssOrder?.NetsisOrderNumber}",
            });

            // Not: İrsaliye otomatik çekme kaldırıldı — sipariş Netsis'e iletildiği anda
            // irsaliye henüz kesilmemiştir. İrsaliye çekimi "İrsaliye Yenile" butonu ile yapılmalıdır.

            // ── Additive: bu sevkiyat bir eksik-tamamlama (followup) ise ilgili
            //    ShortageRecord'ları Shipped yap. Çekirdek export / SkipToDelivered DEĞİŞMEDİ.
            var relatedShortages = await _context.ShortageRecords
                .Where(r => r.FollowupShipmentId == shipment.Id
                         && r.Status == Domain.Enums.ShortageStatus.DispatchRequested)
                .ToListAsync(cancellationToken);
            foreach (var r in relatedShortages)
            {
                r.Status = Domain.Enums.ShortageStatus.Shipped;
                r.ResolvedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return new ExportClothingShipmentToNetsisResult(
                shipment.IssOrder?.NetsisOrderNumber ?? string.Empty,
                null,
                new List<string>());
        }

        private static string? PickBelgeNo(params string?[] candidates)
        {
            foreach (var c in candidates)
                if (!string.IsNullOrWhiteSpace(c) && c != "0") return c;
            return null;
        }
    }
}
