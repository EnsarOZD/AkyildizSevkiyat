using Akyildiz.Sevkiyat.Application.External.Netsis.Dtos;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.Reconciliation.Services;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Netsis.Commands.ExportShipmentToNetsis
{
    public class ExportShipmentToNetsisCommandHandler : IRequestHandler<ExportShipmentToNetsisCommand, string>
    {
        private readonly IApplicationDbContext _context;
        private readonly INetsisClient _netsisClient;
        private readonly ICurrentUserService _currentUserService;
        private readonly ReconciliationGuard _guard;

        public ExportShipmentToNetsisCommandHandler(
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

        public async Task<string> Handle(ExportShipmentToNetsisCommand request, CancellationToken cancellationToken)
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

            // Aktarım için geçerli durumlar: ReadyForDispatch veya sonrası (teslim edilmemiş)
            var exportableStatuses = new[]
            {
                ShipmentStatus.ReadyForDispatch,
                ShipmentStatus.AssignedToVehicle,
            };

            if (!exportableStatuses.Contains(shipment.Status))
                throw new DomainException(
                    $"Sevkiyat Netsis'e aktarılabilir durumda değil. Mevcut durum: {shipment.Status}. " +
                    $"Gerekli durum: ReadyForDispatch veya AssignedToVehicle.");

            // ── Enforcement: Open Error sorunları varsa aktarımı engelle ──────────
            await _guard.ThrowIfOpenErrorIssuesAsync(request.ShipmentId, cancellationToken);

            // Zaten aktarılmışsa yeniden aktarma — iş akışı gerektiriyorsa bu kontrolü kaldırın
            if (shipment.NetsisTransferredAt.HasValue)
                throw new ConflictException(
                    $"Sevkiyat #{shipment.Id} zaten Netsis'e aktarılmış " +
                    $"({shipment.NetsisTransferredAt:dd.MM.yyyy HH:mm}).");

            // Project'te NetsisCariKodu yoksa blokla
            // TODO: NETSIS_API — CariKod mapping net olunca bu kontrolü kesinleştir
            if (string.IsNullOrWhiteSpace(shipment.Project.NetsisCariKodu))
                throw new DomainException(
                    $"Proje '{shipment.Project.Name}' için Netsis Cari Kodu tanımlanmamış. " +
                    "Lütfen proje kaydına NetsisCariKodu ekleyin.");

            var siparisRequest = BuildSiparisRequest(shipment);

            var result = await _netsisClient.CreateSiparisAsync(siparisRequest, cancellationToken);

            if (!result.Basarili)
                throw new DomainException(
                    $"Netsis sipariş aktarımı başarısız: {result.Mesaj ?? "Bilinmeyen hata"}");

            // Aktarım bilgilerini kaydet
            shipment.MarkNetsisTransferred(DateTime.UtcNow);
            shipment.IssOrder.NetsisOrderNumber = result.NetsisOrderNo ?? siparisRequest.BelgeNo;

            // Audit kaydı — sevkiyat zaman çizelgesinde görünür
            _context.ShipmentHistories.Add(new Domain.Entities.ShipmentHistory
            {
                ShipmentId      = shipment.Id,
                OldStatus       = shipment.Status,
                NewStatus       = shipment.Status,
                ChangedAt       = DateTime.UtcNow,
                ChangedByUserId = _currentUserService.UserId,
                Description     = $"Netsis'e aktarıldı. Belge No: {shipment.IssOrder.NetsisOrderNumber}",
            });

            await _context.SaveChangesAsync(cancellationToken);

            return shipment.IssOrder.NetsisOrderNumber;
        }

        private static NetsisSiparisRequest BuildSiparisRequest(Domain.Entities.Shipment shipment)
        {
            // Belge No: ISS-IP talep numarası = Netsis Belge No (toplantı kararı)
            var belgeNo = shipment.TalepNo
                ?? shipment.IssOrder.TalepNo
                ?? shipment.IssOrder.ExternalOrderNumber;

            // NetsisStockCode doğrulaması — boş olanları listele
            var missingNetsisCode = shipment.Lines
                .Where(l => l.StockMaster == null || string.IsNullOrWhiteSpace(l.StockMaster.NetsisStockCode))
                .Select(l => l.StockName)
                .ToList();

            if (missingNetsisCode.Any())
                throw new DomainException(
                    $"Aşağıdaki ürünlerin Netsis Stok Kodu tanımlanmamış: {string.Join(", ", missingNetsisCode)}");

            var lines = shipment.Lines
                .Select((l, idx) => new NetsisSiparisLine
                {
                    SatirNo     = idx + 1,
                    StokKodu    = l.StockMaster!.NetsisStockCode!,
                    Miktar      = l.OrderedQty,
                    Birim       = l.Unit.ToString(),
                    BirimFiyati = l.IssOrderLine?.BirimFiyati ?? 0,
                    KdvOrani    = l.IssOrderLine?.KDVOrani    ?? 0,
                })
                .ToList();

            return new NetsisSiparisRequest
            {
                BelgeNo      = belgeNo,
                CariKodu     = shipment.Project.NetsisCariKodu!,
                ProjeKodu    = shipment.Project.Code,
                TeslimTarihi = shipment.DeliveryDate,
                // EKACK alanları
                SiparisId                     = shipment.Id.ToString(),
                KurumKodu                     = shipment.Project.InstitutionCode,
                TalepNo                       = shipment.TalepNo ?? shipment.IssOrder.TalepNo,
                TalepTuru                     = shipment.IssOrder.TalepTuru,
                Donem                         = shipment.IssOrder.Donem,
                TeslimAlacakKisiler           = shipment.IssOrder.TeslimAlacakKisiler,
                TeslimAlacakTelefonNumaralari = shipment.IssOrder.TeslimAlacakTelefonNumaralari,
                YoneticiMailAdresleri         = shipment.IssOrder.YoneticiMailAdresleri,
                Satirlar                      = lines,
            };
        }
    }
}
