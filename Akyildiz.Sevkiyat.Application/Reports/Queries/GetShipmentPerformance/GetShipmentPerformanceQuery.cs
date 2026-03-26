using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Reports.Queries.GetShipmentPerformance
{
    public record GetShipmentPerformanceQuery(
        DateTime StartDate,
        DateTime EndDate,
        int? ZoneId = null
    ) : IRequest<ShipmentPerformanceDto>;

    public class ShipmentPerformanceDto
    {
        public int TotalDelivered { get; set; }
        public int OnTime { get; set; }
        public int Late { get; set; }
        public double OnTimeRate { get; set; } // percentage
        public List<ShipmentPerformanceRow> Rows { get; set; } = new();
        public List<ZonePerformanceRow> ByZone { get; set; } = new();
    }

    public class ShipmentPerformanceRow
    {
        public int Id { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string? ZoneName { get; set; }
        public string? TalepNo { get; set; }
        public DateTime DeliveryDate { get; set; }
        public DateTime DeliveredAt { get; set; }
        public bool IsLate { get; set; }
        public int DelayDays { get; set; }
        public string? DriverName { get; set; }
    }

    public class ZonePerformanceRow
    {
        public string ZoneName { get; set; } = string.Empty;
        public int Total { get; set; }
        public int OnTime { get; set; }
        public int Late { get; set; }
        public double OnTimeRate { get; set; }
    }

    public class GetShipmentPerformanceQueryHandler
        : IRequestHandler<GetShipmentPerformanceQuery, ShipmentPerformanceDto>
    {
        private readonly IApplicationDbContext _context;

        public GetShipmentPerformanceQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ShipmentPerformanceDto> Handle(
            GetShipmentPerformanceQuery request, CancellationToken cancellationToken)
        {
            var start = request.StartDate.Date;
            var end   = request.EndDate.Date.AddDays(1);

            var query = _context.Shipments
                .Include(s => s.Project).ThenInclude(p => p.Zone)
                .Where(s => s.Status == ShipmentStatus.Delivered
                    && s.DeliveredAt.HasValue
                    && s.DeliveredAt >= start
                    && s.DeliveredAt < end);

            if (request.ZoneId.HasValue)
                query = query.Where(s => s.Project.ZoneId == request.ZoneId);

            var shipments = await query
                .OrderBy(s => s.DeliveredAt)
                .ToListAsync(cancellationToken);

            var rows = shipments.Select(s =>
            {
                var deliveredAt = s.DeliveredAt!.Value;
                var delayDays = (int)(deliveredAt.Date - s.DeliveryDate.Date).TotalDays;
                return new ShipmentPerformanceRow
                {
                    Id           = s.Id,
                    ProjectName  = s.Project.Name,
                    ZoneName     = s.Project.Zone?.Name,
                    TalepNo      = s.TalepNo,
                    DeliveryDate = s.DeliveryDate,
                    DeliveredAt  = deliveredAt,
                    IsLate       = delayDays > 0,
                    DelayDays    = delayDays > 0 ? delayDays : 0,
                    DriverName   = s.AssignedDriverName,
                };
            }).ToList();

            var byZone = rows
                .GroupBy(r => r.ZoneName ?? "Bölgesiz")
                .Select(g => new ZonePerformanceRow
                {
                    ZoneName  = g.Key,
                    Total     = g.Count(),
                    OnTime    = g.Count(r => !r.IsLate),
                    Late      = g.Count(r => r.IsLate),
                    OnTimeRate = g.Count() == 0 ? 0
                        : Math.Round(g.Count(r => !r.IsLate) * 100.0 / g.Count(), 1),
                })
                .OrderByDescending(r => r.Total)
                .ToList();

            var total   = rows.Count;
            var onTime  = rows.Count(r => !r.IsLate);

            return new ShipmentPerformanceDto
            {
                TotalDelivered = total,
                OnTime         = onTime,
                Late           = total - onTime,
                OnTimeRate     = total == 0 ? 0 : Math.Round(onTime * 100.0 / total, 1),
                Rows           = rows,
                ByZone         = byZone,
            };
        }
    }
}
