using MediatR;
using Microsoft.EntityFrameworkCore;
using Akyildiz.Sevkiyat.Application.Interfaces;

namespace Akyildiz.Sevkiyat.Application.Admin.Queries.GetDriverSessions
{
    public class GetDriverSessionsQueryHandler
        : IRequestHandler<GetDriverSessionsQuery, GetDriverSessionsResult>
    {
        private readonly IApplicationDbContext _context;

        public GetDriverSessionsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GetDriverSessionsResult> Handle(
            GetDriverSessionsQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.DriverSessions
                .Include(ds => ds.Driver)
                .Include(ds => ds.Vehicle)
                .Where(ds => ds.StartTime.Date >= request.FromDate.Date &&
                             ds.StartTime.Date <= request.ToDate.Date);

            if (request.DriverId.HasValue)
                query = query.Where(ds => ds.DriverId == request.DriverId.Value);

            if (request.VehicleId.HasValue)
                query = query.Where(ds => ds.VehicleId == request.VehicleId.Value);

            if (request.Status.HasValue)
                query = query.Where(ds => ds.Status == request.Status.Value);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(ds => ds.StartTime)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(ds => new DriverSessionDto(
                    ds.Id,
                    ds.DriverId,
                    ds.Driver.FullName,
                    ds.VehicleId,
                    ds.Vehicle.PlateNumber,
                    ds.StartTime,
                    ds.EndTime,
                    ds.TotalDurationMinutes,
                    ds.StartLatitude,
                    ds.StartLongitude,
                    ds.EndLatitude,
                    ds.EndLongitude,
                    ds.Status,
                    ds.Notes,
                    ds.StartOdometerKm,
                    ds.EndOdometerKm,
                    ds.Shipments
                        .Select(m => new SessionShipmentDto(
                            m.Shipment.Id,
                            m.Shipment.Project.Name,
                            m.Shipment.TalepNo,
                            m.Shipment.IrsaliyeNo,
                            m.Shipment.Status.ToString()))
                        .ToList()))
                .ToListAsync(cancellationToken);

            return new GetDriverSessionsResult(items, totalCount, request.PageNumber, request.PageSize);
        }
    }
}
