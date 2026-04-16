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

            if (shipment.Status != ShipmentStatus.Created)
                throw new DomainException(
                    $"Kıyafet Netsis aktarımı sadece 'Oluşturuldu' durumundaki sevkiyatlar için geçerlidir. " +
                    $"Mevcut durum: {shipment.Status}");

            if (shipment.NetsisTransferredAt.HasValue)
                throw new Akyildiz.Sevkiyat.Domain.Exceptions.ConflictException(
                    $"Sevkiyat #{shipment.Id} zaten Netsis'e aktarılmış " +
                    $"({shipment.NetsisTransferredAt:dd.MM.yyyy HH:mm}).");

            if (string.IsNullOrWhiteSpace(shipment.Project.NetsisCariKodu))
                throw new DomainException(
                    $"Proje '{shipment.Project.Name}' için Netsis Cari Kodu tanımlanmamış.");

            // NetsisStockCode doğrulaması
            var missingNetsisCode = shipment.Lines
                .Where(l => l.StockMaster == null || string.IsNullOrWhiteSpace(l.StockMaster.NetsisStockCode))
                .Select(l => l.StockName)
                .ToList();

            if (missingNetsisCode.Any())
                throw new DomainException(
                    $"Aşağıdaki ürünlerin Netsis Stok Kodu tanımlanmamış: {string.Join(", ", missingNetsisCode)}");

            var belgeNo = PickBelgeNo(
                shipment.IssOrder?.ExternalOrderNumber,
                shipment.TalepNo,
                shipment.IssOrder?.TalepNo);

            // Kıyafet operasyonunda ISS orijinal miktarı kullanılır
            var lines = shipment.Lines
                .Select((l, idx) => new NetsisSiparisLine
                {
                    SatirNo     = idx + 1,
                    StokKodu    = l.StockMaster!.NetsisStockCode!,
                    Miktar      = l.IssOrderLine?.OrderedQty ?? l.OrderedQty,
                    Birim       = l.Unit.ToString(),
                    BirimFiyati = l.IssOrderLine?.BirimFiyati ?? 0,
                    KdvOrani    = l.IssOrderLine?.KDVOrani    ?? 0,
                })
                .ToList();

            var siparisRequest = new NetsisSiparisRequest
            {
                BelgeNo      = belgeNo,
                CariKodu     = shipment.Project.NetsisCariKodu!,
                ProjeKodu    = shipment.Project.NetsisTeslimCariKodu ?? shipment.Project.Code,
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
