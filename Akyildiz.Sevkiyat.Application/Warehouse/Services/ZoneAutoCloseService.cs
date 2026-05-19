using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Services
{
    /// <summary>
    /// Tüm sevkiyatlar final duruma (Delivered, ReturnedToWarehouse, Cancelled, Passive) geçince
    /// zone'u otomatik olarak Dispatched'e kapatır. Teslim, iade, iptal ve pasif işlemleri sonrası çağrılır.
    /// </summary>
    public class ZoneAutoCloseService
    {
        private readonly IApplicationDbContext _context;

        public ZoneAutoCloseService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task TryAutoCloseAsync(int zonePreparationId, CancellationToken ct)
        {
            var zp = await _context.ZonePreparations
                .FirstOrDefaultAsync(z => z.Id == zonePreparationId, ct);

            if (zp == null || zp.Status == ZonePreparationStatus.Dispatched)
                return;

            // Final durumlar: Delivered, ReturnedToWarehouse, Cancelled, Passive
            // Bu statülerin dışında aktif sevkiyat varsa zone kapanmaz.
            var hasActive = await _context.WarehouseShipments
                .AnyAsync(s =>
                    s.ZonePreparationId == zonePreparationId &&
                    s.Status != ShipmentStatus.Cancelled &&
                    s.Status != ShipmentStatus.Passive &&
                    s.Status != ShipmentStatus.Delivered &&
                    s.Status != ShipmentStatus.ReturnedToWarehouse,
                    ct);

            if (!hasActive)
                zp.Status = ZonePreparationStatus.Dispatched;
        }
    }
}
