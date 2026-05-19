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

            var driverIds = sessions.Select(s => s.DriverId).Distinct().ToList();

            var shipments = await _context.Shipments
                .Include(s => s.Project)
                .Include(s => s.IssOrder)
                .Where(s => s.AssignedDriverId.HasValue
                         && driverIds.Contains(s.AssignedDriverId.Value)
                         && (s.Status == ShipmentStatus.AssignedToVehicle
                          || s.Status == ShipmentStatus.Dispatched))
                .ToListAsync(cancellationToken);

            var shipmentsByDriver = shipments
                .GroupBy(s => s.AssignedDriverId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var now = DateTime.UtcNow;

            return sessions.Select(session => new ActiveSessionWithShipmentsDto(
                session.Id,
                session.DriverId,
                session.Driver.FullName,
                session.VehicleId,
                session.Vehicle.PlateNumber,
                session.StartTime,
                (int)(now - session.StartTime).TotalMinutes,
                shipmentsByDriver.TryGetValue(session.DriverId, out var driverShipments)
                    ? driverShipments.Select(s => new StuckShipmentDto(
                        s.Id,
                        s.Project.Name,
                        s.TalepNo,
                        s.IssOrder.ExternalOrderNumber,
                        s.Status.ToString(),
                        s.Lines.Count))
                      .ToList()
                    : []
            )).ToList();
        }
    }
}
