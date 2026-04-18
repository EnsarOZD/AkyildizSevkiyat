using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Services
{
    /// <summary>
    /// After a shipment is removed or passived, checks if the owning ZonePrep has
    /// zero remaining active shipments and auto-closes it (→ Dispatched).
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

            var hasActive = await _context.Shipments
                .AnyAsync(s =>
                    s.ZonePreparationId == zonePreparationId &&
                    s.Status != ShipmentStatus.Cancelled &&
                    s.Status != ShipmentStatus.Passive &&
                    s.Status != ShipmentStatus.Created,
                    ct);

            if (!hasActive)
                zp.Status = ZonePreparationStatus.Dispatched;
        }
    }
}
