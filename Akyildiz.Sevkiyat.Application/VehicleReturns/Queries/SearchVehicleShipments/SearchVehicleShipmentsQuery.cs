using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.VehicleReturns.Queries.SearchVehicleShipments
{
    public record SearchVehicleShipmentsQuery(Guid SessionId, string SearchTerm)
        : IRequest<List<VehicleShipmentDto>>;

    public class VehicleShipmentDto
    {
        public int Id { get; set; }
        public string? IrsaliyeNo { get; set; }
        public string? OrderNumber { get; set; }
        public string? TalepNo { get; set; }
        public string ProjectCode { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class SearchVehicleShipmentsQueryHandler : IRequestHandler<SearchVehicleShipmentsQuery, List<VehicleShipmentDto>>
    {
        private readonly IApplicationDbContext _context;

        public SearchVehicleShipmentsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<VehicleShipmentDto>> Handle(SearchVehicleShipmentsQuery request, CancellationToken cancellationToken)
        {
            var session = await _context.DriverSessions
                .Include(ds => ds.Vehicle)
                .FirstOrDefaultAsync(ds => ds.Id == request.SessionId, cancellationToken);

            if (session == null)
                throw new NotFoundException("DriverSession", request.SessionId);

            var plateNumber = session.Vehicle.PlateNumber;
            var term = request.SearchTerm.Trim();

            var shipments = await _context.Shipments
                .Include(s => s.IssOrder)
                .Include(s => s.Project)
                .Where(s => s.AssignedPlateNumber == plateNumber &&
                    (EF.Functions.Like(s.IrsaliyeNo ?? "", $"%{term}%") ||
                     EF.Functions.Like(s.IssOrder != null ? s.IssOrder.ExternalOrderNumber : "", $"%{term}%") ||
                     EF.Functions.Like(s.IssOrder != null ? (s.IssOrder.TalepNo ?? "") : "", $"%{term}%") ||
                     EF.Functions.Like(s.Project.Code, $"%{term}%") ||
                     EF.Functions.Like(s.Project.Name, $"%{term}%")))
                .OrderByDescending(s => s.Id)
                .Take(20)
                .Select(s => new VehicleShipmentDto
                {
                    Id = s.Id,
                    IrsaliyeNo = s.IrsaliyeNo,
                    OrderNumber = s.IssOrder != null ? s.IssOrder.ExternalOrderNumber : null,
                    TalepNo = s.IssOrder != null ? s.IssOrder.TalepNo : null,
                    ProjectCode = s.Project.Code,
                    ProjectName = s.Project.Name,
                    Status = s.Status.ToString()
                })
                .ToListAsync(cancellationToken);

            return shipments;
        }
    }
}
