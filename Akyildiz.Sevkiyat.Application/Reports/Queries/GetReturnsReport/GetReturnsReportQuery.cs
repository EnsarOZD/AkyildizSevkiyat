using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Reports.Queries.GetReturnsReport
{
    public record GetReturnsReportQuery(
        DateTime StartDate,
        DateTime EndDate,
        int? ZoneId = null
    ) : IRequest<ReturnsReportDto>;

    public class ReturnsReportDto
    {
        public int TotalReturnedLines { get; set; }
        public decimal TotalReturnedQty { get; set; }
        public List<ReturnReasonSummary> ByReason { get; set; } = new();
        public List<ReturnRow> Rows { get; set; } = new();
    }

    public class ReturnReasonSummary
    {
        public string Reason { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalQty { get; set; }
    }

    public class ReturnRow
    {
        public int ShipmentId { get; set; }
        public string? TalepNo { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string? ZoneName { get; set; }
        public DateTime? ReturnedAt { get; set; }
        public string StockCode { get; set; } = string.Empty;
        public string StockName { get; set; } = string.Empty;
        public decimal ReturnedQty { get; set; }
        public string? ReturnReason { get; set; }
        public string? ReturnNote { get; set; }
    }

    public class GetReturnsReportQueryHandler
        : IRequestHandler<GetReturnsReportQuery, ReturnsReportDto>
    {
        private readonly IApplicationDbContext _context;

        public GetReturnsReportQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ReturnsReportDto> Handle(
            GetReturnsReportQuery request, CancellationToken cancellationToken)
        {
            var start = request.StartDate.Date;
            var end   = request.EndDate.Date.AddDays(1);

            var query = _context.ShipmentLines
                .Include(l => l.Shipment)
                    .ThenInclude(s => s.Project)
                        .ThenInclude(p => p.Zone)
                .Where(l => l.ReturnedQty > 0
                    && l.Shipment.ReturnedAt.HasValue
                    && l.Shipment.ReturnedAt >= start
                    && l.Shipment.ReturnedAt < end);

            if (request.ZoneId.HasValue)
                query = query.Where(l => l.Shipment.Project.ZoneId == request.ZoneId);

            var lines = await query
                .OrderByDescending(l => l.Shipment.ReturnedAt)
                .ToListAsync(cancellationToken);

            var rows = lines.Select(l => new ReturnRow
            {
                ShipmentId  = l.ShipmentId,
                TalepNo     = l.Shipment.TalepNo,
                ProjectName = l.Shipment.Project.Name,
                ZoneName    = l.Shipment.Project.Zone?.Name,
                ReturnedAt  = l.Shipment.ReturnedAt,
                StockCode   = l.StockCode,
                StockName   = l.StockName,
                ReturnedQty = l.ReturnedQty ?? 0,
                ReturnReason = l.ReturnReason?.ToString(),
                ReturnNote  = l.Shipment.ReturnNote,
            }).ToList();

            var byReason = rows
                .GroupBy(r => r.ReturnReason ?? "Belirtilmemiş")
                .Select(g => new ReturnReasonSummary
                {
                    Reason   = g.Key,
                    Count    = g.Count(),
                    TotalQty = g.Sum(r => r.ReturnedQty),
                })
                .OrderByDescending(r => r.Count)
                .ToList();

            return new ReturnsReportDto
            {
                TotalReturnedLines = rows.Count,
                TotalReturnedQty   = rows.Sum(r => r.ReturnedQty),
                ByReason           = byReason,
                Rows               = rows,
            };
        }
    }
}
