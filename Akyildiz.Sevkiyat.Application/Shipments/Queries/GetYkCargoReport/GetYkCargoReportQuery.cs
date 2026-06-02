using MediatR;
using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Common.Models;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Shipments.Queries.GetYkCargoReport
{
    public class YkCargoReportItemDto
    {
        public int Id { get; set; }
        public string ProjectCode { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string? ExternalOrderNumber { get; set; }
        public string? TalepNo { get; set; }
        public string ShipmentStatus { get; set; } = string.Empty;
        public DateTime DeliveryDate { get; set; }
        public DateTime? DispatchedAt { get; set; }

        public string? YkCargoKey { get; set; }
        public int? YkJobId { get; set; }
        public string? YkBarcode { get; set; }
        public string? YkOperationStatus { get; set; }
        public string? YkOperationMessage { get; set; }
        public string? YkErrorCode { get; set; }
        public string? YkErrorMessage { get; set; }
        public DateTime? YkLastQueryAt { get; set; }
    }

    public class GetYkCargoReportQuery : IRequest<PaginatedList<YkCargoReportItemDto>>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting", "Dispatcher" };

        public string? Search { get; set; }
        public string? YkStatus { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class GetYkCargoReportQueryHandler : IRequestHandler<GetYkCargoReportQuery, PaginatedList<YkCargoReportItemDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetYkCargoReportQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedList<YkCargoReportItemDto>> Handle(GetYkCargoReportQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Shipments
                .Include(s => s.Project)
                .Include(s => s.IssOrder)
                .Where(s => s.YkCargoKey != null)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var s = request.Search.Trim().ToLower();
                query = query.Where(x =>
                    (x.Project.Name != null && x.Project.Name.ToLower().Contains(s)) ||
                    (x.Project.Code != null && x.Project.Code.ToLower().Contains(s)) ||
                    (x.IssOrder != null && x.IssOrder.ExternalOrderNumber != null && x.IssOrder.ExternalOrderNumber.ToLower().Contains(s)) ||
                    (x.TalepNo != null && x.TalepNo.ToLower().Contains(s)) ||
                    (x.YkCargoKey != null && x.YkCargoKey.ToLower().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(request.YkStatus))
                query = query.Where(x => x.YkOperationStatus == request.YkStatus);

            if (request.StartDate.HasValue)
                query = query.Where(x => x.DeliveryDate >= request.StartDate.Value);

            if (request.EndDate.HasValue)
                query = query.Where(x => x.DeliveryDate <= request.EndDate.Value);

            query = query.OrderByDescending(s => s.DispatchedAt ?? s.DeliveryDate);

            return await PaginatedList<YkCargoReportItemDto>.CreateAsync(
                query.Select(s => new YkCargoReportItemDto
                {
                    Id                 = s.Id,
                    ProjectCode        = s.Project.Code,
                    ProjectName        = s.Project.Name,
                    ExternalOrderNumber = s.IssOrder != null ? s.IssOrder.ExternalOrderNumber : null,
                    TalepNo            = s.TalepNo,
                    ShipmentStatus     = s.Status.ToString(),
                    DeliveryDate       = s.DeliveryDate,
                    DispatchedAt       = s.DispatchedAt,
                    YkCargoKey         = s.YkCargoKey,
                    YkJobId            = s.YkJobId,
                    YkBarcode          = s.YkBarcode,
                    YkOperationStatus  = s.YkOperationStatus,
                    YkOperationMessage = s.YkOperationMessage,
                    YkErrorCode        = s.YkErrorCode,
                    YkErrorMessage     = s.YkErrorMessage,
                    YkLastQueryAt      = s.YkLastQueryAt,
                }),
                request.PageNumber,
                request.PageSize);
        }
    }
}
