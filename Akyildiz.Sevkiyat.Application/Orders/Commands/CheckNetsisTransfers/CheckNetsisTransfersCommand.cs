using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Akyildiz.Sevkiyat.Application.Orders.Commands.CheckNetsisTransfers
{
    public record CheckNetsisTransfersCommand : IRequest<CheckNetsisTransfersResult>
    {
        /// <summary>Netsis sorgusunun başlangıç tarihi. Boş ise son 30 gün kullanılır.</summary>
        public DateTime? FromDate { get; init; }
        /// <summary>Netsis sorgusunun bitiş tarihi (dahil). Boş ise üst sınır yok.</summary>
        public DateTime? ToDate { get; init; }
        /// <summary>
        /// true → aktarılmış (IsTransferred=true) siparişler de Netsis'te kontrol edilir;
        /// silinmiş olanlar geri alınır. Ağ yoğun işlem — yalnızca kullanıcı elle tetiklediğinde true.
        /// false (default) → sadece bekleyen (IsTransferred=false) siparişler kontrol edilir.
        /// </summary>
        public bool CheckTransferred { get; init; } = false;
    }

    public class CheckNetsisTransfersResult
    {
        public int Checked { get; set; }
        public int MarkedAsTransferred { get; set; }
        public int ResetToActive { get; set; }
        /// <summary>
        /// Aktarılmış görünen ama Netsis'te artık bulunamayan sipariş sayısı.
        /// Bu siparişlerin IsTransferred bayrağı false'a çekildi.
        /// Yalnızca explicit tarih aralıklı kontrolde doldurulur.
        /// </summary>
        public int NetsisDeletedCount { get; set; }
        public int TotalPending { get; set; }
        public string? Error { get; set; }
    }

    public class CheckNetsisTransfersCommandHandler
        : IRequestHandler<CheckNetsisTransfersCommand, CheckNetsisTransfersResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly INetsisClient _netsis;
        private readonly ILogger<CheckNetsisTransfersCommandHandler> _logger;

        public CheckNetsisTransfersCommandHandler(
            IApplicationDbContext context,
            INetsisClient netsis,
            ILogger<CheckNetsisTransfersCommandHandler> logger)
        {
            _context = context;
            _netsis  = netsis;
            _logger  = logger;
        }

        public async Task<CheckNetsisTransfersResult> Handle(
            CheckNetsisTransfersCommand request,
            CancellationToken cancellationToken)
        {
            // Orphan düzeltme: IsTransferred=true AMA bağlı tüm sevkiyatlar iptal edilmiş
            // (Netsis'ten gelen, local shipment'ı olmayan kayıtlara dokunma)
            var orphaned = await _context.IssOrders
                .Where(o => o.IsTransferred
                    && _context.Shipments.Any(s => s.IssOrderId == o.Id)
                    && !_context.Shipments.Any(s => s.IssOrderId == o.Id
                                                 && s.Status != ShipmentStatus.Cancelled))
                .ToListAsync(cancellationToken);

            foreach (var o in orphaned)
                o.IsTransferred = false;

            if (orphaned.Count > 0)
                await _context.SaveChangesAsync(cancellationToken);

            // Tarih aralığı: manuel çağrıda kullanıcının seçimi, arka planda son 30 gün
            var fromUtc = request.FromDate?.Date ?? DateTime.UtcNow.AddDays(-30).Date;
            var toUtcEx = request.ToDate.HasValue ? request.ToDate.Value.Date.AddDays(1) : (DateTime?)null;

            // IsTransferred=false olan siparişler (her zaman kontrol edilir)
            var pendingOrders = await _context.IssOrders
                .Where(o => !o.IsTransferred
                    && o.OrderDate >= fromUtc
                    && (toUtcEx == null || o.OrderDate < toUtcEx))
                .OrderBy(o => o.Id)
                .Select(o => new { o.Id, o.ExternalOrderNumber })
                .ToListAsync(cancellationToken);

            // IsTransferred=true → sadece kullanıcı elle tetiklediğinde kontrol edilir
            // Arka plan çağrısında (CheckTransferred=false) atlanır — sunucu yükü önlemi
            List<(int Id, string ExternalOrderNumber)> transferredOrders;
            if (request.CheckTransferred)
            {
                var rawTransferred = await _context.IssOrders
                    .Where(o => o.IsTransferred
                        && o.OrderDate >= fromUtc
                        && (toUtcEx == null || o.OrderDate < toUtcEx))
                    .OrderBy(o => o.Id)
                    .Select(o => new { o.Id, o.ExternalOrderNumber })
                    .ToListAsync(cancellationToken);
                transferredOrders = rawTransferred
                    .Select(o => (o.Id, o.ExternalOrderNumber))
                    .ToList();
            }
            else
            {
                transferredOrders = new List<(int, string)>();
            }

            var totalPending = pendingOrders.Count;

            if (!pendingOrders.Any() && !transferredOrders.Any())
                return new CheckNetsisTransfersResult { ResetToActive = orphaned.Count };

            // Tüm kontrol edilecek sipariş numaralarını birleştir (duplicate'leri temizle)
            var allNumbers = pendingOrders.Select(o => o.ExternalOrderNumber)
                .Concat(transferredOrders.Select(o => o.ExternalOrderNumber))
                .Distinct()
                .ToList();

            _logger.LogInformation(
                "CheckNetsisTransfers: {Pending} bekleyen + {Transferred} aktarılmış = {Total} sipariş Netsis'te kontrol ediliyor.",
                pendingOrders.Count, transferredOrders.Count, allNumbers.Count);

            // Netsis'te hangilerinin mevcut olduğunu sorgula
            var (existingInNetsis, netsisError) = await _netsis.CheckOrdersExistInNetsisAsync(
                allNumbers, cancellationToken);

            if (!existingInNetsis.Any())
                return new CheckNetsisTransfersResult
                {
                    Checked       = netsisError != null ? 0 : allNumbers.Count,
                    TotalPending  = totalPending,
                    ResetToActive = orphaned.Count,
                    Error         = netsisError,
                };

            // IsTransferred=false → Netsis'te bulundu → IsTransferred=true yap
            var matchedPendingIds = pendingOrders
                .Where(o => existingInNetsis.Contains(o.ExternalOrderNumber))
                .Select(o => o.Id)
                .ToHashSet();

            if (matchedPendingIds.Any())
            {
                var toMarkTransferred = await _context.IssOrders
                    .Where(o => matchedPendingIds.Contains(o.Id))
                    .ToListAsync(cancellationToken);
                foreach (var order in toMarkTransferred)
                    order.IsTransferred = true;
            }

            // IsTransferred=true → Netsis'te BULUNMADI → geri al (silinmiş)
            int netsisDeletedCount = 0;
            if (transferredOrders.Any())
            {
                var deletedInNetsisIds = transferredOrders
                    .Where(o => !existingInNetsis.Contains(o.ExternalOrderNumber))
                    .Select(o => o.Id)
                    .ToHashSet();

                if (deletedInNetsisIds.Any())
                {
                    var toResetOrders = await _context.IssOrders
                        .Where(o => deletedInNetsisIds.Contains(o.Id))
                        .ToListAsync(cancellationToken);

                    // Bağlı sevkiyatların Netsis aktarım bilgilerini de sıfırla
                    var toResetShipments = await _context.Shipments
                        .Where(s => deletedInNetsisIds.Contains(s.IssOrderId)
                                 && s.NetsisTransferredAt != null)
                        .ToListAsync(cancellationToken);

                    foreach (var order in toResetOrders)
                    {
                        order.IsTransferred = false;
                        order.NetsisOrderNumber = null;
                    }
                    foreach (var shipment in toResetShipments)
                        shipment.RevertNetsisTransfer();

                    netsisDeletedCount = toResetOrders.Count;

                    _logger.LogWarning(
                        "CheckNetsisTransfers: {Count} sipariş Netsis'te bulunamadı ve sıfırlandı ({Shipments} sevkiyat dahil). IDs: {Ids}",
                        netsisDeletedCount,
                        toResetShipments.Count,
                        string.Join(", ", deletedInNetsisIds));
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            return new CheckNetsisTransfersResult
            {
                Checked             = allNumbers.Count,
                TotalPending        = totalPending,
                MarkedAsTransferred = matchedPendingIds.Count,
                ResetToActive       = orphaned.Count,
                NetsisDeletedCount  = netsisDeletedCount,
                Error               = netsisError
            };
        }
    }
}
