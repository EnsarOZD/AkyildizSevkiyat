using MediatR;
using Microsoft.EntityFrameworkCore;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;

namespace Akyildiz.Sevkiyat.Application.Admin.Queries.GetActiveSessionsWithShipments
{
    public class GetActiveSessionsWithShipmentsQueryHandler
        : IRequestHandler<GetActiveSessionsWithShipmentsQuery, List<ActiveSessionWithShipmentsDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetActiveSessionsWithShipmentsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ActiveSessionWithShipmentsDto>> Handle(
            GetActiveSessionsWithShipmentsQuery request,
            CancellationToken cancellationToken)
        {
            var sessions = await _context.DriverSessions
                .Include(ds => ds.Driver)
                .Include(ds => ds.Vehicle)
                .Where(ds => ds.Status == DriverSessionStatus.Open)
                .OrderBy(ds => ds.StartTime)
                .ToListAsync(cancellationToken);

            if (sessions.Count == 0)
                return [];

            var now = DateTime.UtcNow;
            var today = now.Date;
            var sessionIds = sessions.Select(s => s.Id).ToList();

            // Açık seferin MANİFESTİ (DriverSessionShipments) = doğru kapsam.
            // Şoföre atanmış tüm sevkiyatlar değil — yalnızca bu seferde taşınanlar.
            var manifest = await _context.DriverSessionShipments
                .Where(m => sessionIds.Contains(m.DriverSessionId))
                .Select(m => new { m.DriverSessionId, m.ShipmentId })
                .ToListAsync(cancellationToken);

            var manifestBySession = manifest
                .GroupBy(m => m.DriverSessionId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ShipmentId).ToList());

            var manifestShipmentIds = manifest.Select(m => m.ShipmentId).Distinct().ToList();

            // Manifesti boş olan eski/geçiş seferleri için şoför+durum bazlı yedek kapsam.
            var fallbackDriverIds = sessions
                .Where(s => !manifestBySession.ContainsKey(s.Id))
                .Select(s => s.DriverId)
                .Distinct()
                .ToList();

            var manifestShipments = manifestShipmentIds.Count > 0
                ? await _context.Shipments
                    .Include(s => s.Project)
                    .Include(s => s.IssOrder)
                    .Include(s => s.Lines)
                    .Where(s => manifestShipmentIds.Contains(s.Id))
                    .ToListAsync(cancellationToken)
                : new();

            var fallbackShipments = fallbackDriverIds.Count > 0
                ? await _context.Shipments
                    .Include(s => s.Project)
                    .Include(s => s.IssOrder)
                    .Include(s => s.Lines)
                    .Where(s => s.AssignedDriverId.HasValue
                             && fallbackDriverIds.Contains(s.AssignedDriverId.Value)
                             && (s.Status == ShipmentStatus.AssignedToVehicle
                              || s.Status == ShipmentStatus.Dispatched
                              || (s.Status == ShipmentStatus.Delivered
                                  && s.DeliveredAt.HasValue && s.DeliveredAt.Value.Date == today)))
                    .ToListAsync(cancellationToken)
                : new();

            var manifestById = manifestShipments.ToDictionary(s => s.Id);

            return sessions.Select(session =>
            {
                // Bu seferin sevkiyatları: manifest varsa ondan, yoksa şoför bazlı yedek
                var sessionShipments = manifestBySession.TryGetValue(session.Id, out var ids)
                    ? ids.Where(manifestById.ContainsKey).Select(id => manifestById[id]).ToList()
                    : fallbackShipments.Where(s => s.AssignedDriverId == session.DriverId).ToList();

                // Operasyon ekranı için aktif (teslim edilmemiş) sevkiyatlar
                var activeShipments = sessionShipments
                    .Where(s => s.Status != ShipmentStatus.Delivered)
                    .Select(s => new StuckShipmentDto(
                        s.Id, s.ProjectId, s.Project.Name, s.TalepNo, s.IssOrder?.ExternalOrderNumber,
                        s.Status.ToString(), s.Lines.Count))
                    .ToList();

                // Proje bazında duraklar (özet + ilerleme haritası)
                var stops = sessionShipments
                    .GroupBy(s => s.ProjectId)
                    .Select(g =>
                    {
                        var p = g.First().Project;
                        return new ActiveSessionStopDto(
                            p.Id, p.Name, p.Address, p.Latitude, p.Longitude,
                            g.All(s => s.Status == ShipmentStatus.Delivered));
                    })
                    .ToList();

                return new ActiveSessionWithShipmentsDto(
                    session.Id,
                    session.DriverId,
                    session.Driver.FullName,
                    session.VehicleId,
                    session.Vehicle.PlateNumber,
                    session.StartTime,
                    (int)(now - session.StartTime).TotalMinutes,
                    activeShipments,
                    stops.Count,
                    stops.Count(s => s.IsDelivered),
                    stops);
            }).ToList();
        }
    }
}
