using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Reports.Queries.GetShipmentSummary
{
    public record GetShipmentSummaryQuery(
        DateTime StartDate,
        DateTime EndDate,
        int? ZoneId = null
    ) : IRequest<ShipmentSummaryDto>;

    public class ShipmentSummaryDto
    {
        public int Total { get; set; }
        public int Created { get; set; }
        public int AssignedToWarehouse { get; set; }
        public int Picking { get; set; }
        public int ReadyForDispatch { get; set; }
        public int AssignedToVehicle { get; set; }
        public int Delivered { get; set; }
        public int Cancelled { get; set; }
        public int Passive { get; set; }
        public List<ShipmentSummaryRow> Rows { get; set; } = new();
    }

    public class ShipmentSummaryRow
    {
        public int Id { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string? ZoneName { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime DeliveryDate { get; set; }
        public string? DriverName { get; set; }
        public string? TalepNo { get; set; }
        public int LineCount { get; set; }
    }

    public class GetShipmentSummaryQueryHandler : IRequestHandler<GetShipmentSummaryQuery, ShipmentSummaryDto>
    {
        private readonly IApplicationDbContext _context;

        public GetShipmentSummaryQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ShipmentSummaryDto> Handle(GetShipmentSummaryQuery request, CancellationToken cancellationToken)
        {
            var start = request.StartDate.Date;
            var end   = request.EndDate.Date.AddDays(1);

            var query = _context.Shipments
                .Include(s => s.Project).ThenInclude(p => p.Zone)
                .Include(s => s.Lines)
                .Where(s => s.DeliveryDate >= start && s.DeliveryDate < end);

            if (request.ZoneId.HasValue)
                query = query.Where(s => s.Project.ZoneId == request.ZoneId);

            var shipments = await query
                .OrderBy(s => s.DeliveryDate)
                .ToListAsync(cancellationToken);

            var rows = shipments.Select(s => new ShipmentSummaryRow
            {
                Id           = s.Id,
                ProjectName  = s.Project.Name,
                ZoneName     = s.Project.Zone?.Name,
                Status       = s.Status.ToString(),
                DeliveryDate = s.DeliveryDate,
                DriverName   = s.AssignedDriverName,
                TalepNo      = s.TalepNo,
                LineCount    = s.Lines.Count
            }).ToList();

            return new ShipmentSummaryDto
            {
                Total              = rows.Count,
                Created            = rows.Count(r => r.Status == ShipmentStatus.Created.ToString()),
                AssignedToWarehouse= rows.Count(r => r.Status == ShipmentStatus.AssignedToWarehouse.ToString()),
                Picking            = rows.Count(r => r.Status == ShipmentStatus.Picking.ToString()),
                ReadyForDispatch   = rows.Count(r => r.Status == ShipmentStatus.ReadyForDispatch.ToString()),
                AssignedToVehicle  = rows.Count(r => r.Status == ShipmentStatus.AssignedToVehicle.ToString()),
                Delivered          = rows.Count(r => r.Status == ShipmentStatus.Delivered.ToString()),
                Cancelled          = rows.Count(r => r.Status == ShipmentStatus.Cancelled.ToString()),
                Passive            = rows.Count(r => r.Status == ShipmentStatus.Passive.ToString()),
                Rows               = rows
            };
        }
    }
}
