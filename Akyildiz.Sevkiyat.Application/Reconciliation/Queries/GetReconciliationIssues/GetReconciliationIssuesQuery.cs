using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Reconciliation.Queries.GetReconciliationIssues
{
    public class ReconciliationIssueDto
    {
        public int Id { get; set; }
        public ReconciliationCheckType CheckType { get; set; }
        public string CheckTypeName { get; set; } = null!;
        public ReconciliationSeverity Severity { get; set; }
        public ReconciliationStatus Status { get; set; }

        public int? ShipmentId { get; set; }
        public int? ShipmentLineId { get; set; }
        public int? IssOrderLineId { get; set; }

        public string Description { get; set; } = null!;
        public string? ExpectedValue { get; set; }
        public string? ActualValue { get; set; }

        public DateTime DetectedAt { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public string? AcknowledgementNote { get; set; }
    }

    public class ReconciliationIssuePageDto
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<ReconciliationIssueDto> Items { get; set; } = new();

        /// <summary>Özet: her check tipi için Open sorun sayısı</summary>
        public Dictionary<string, int> OpenSummary { get; set; } = new();
    }

    public record GetReconciliationIssuesQuery(
        ReconciliationCheckType? CheckType = null,
        ReconciliationStatus?    Status    = null,
        ReconciliationSeverity?  Severity  = null,
        DateTime?                FromDate  = null,
        DateTime?                ToDate    = null,
        int Page                           = 1,
        int PageSize                       = 50
    ) : IRequest<ReconciliationIssuePageDto>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting" };
    }

    public class GetReconciliationIssuesQueryHandler
        : IRequestHandler<GetReconciliationIssuesQuery, ReconciliationIssuePageDto>
    {
        private readonly IApplicationDbContext _context;

        public GetReconciliationIssuesQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ReconciliationIssuePageDto> Handle(
            GetReconciliationIssuesQuery request, CancellationToken ct)
        {
            var query = _context.ReconciliationIssues.AsQueryable();

            if (request.CheckType.HasValue)
                query = query.Where(i => i.CheckType == request.CheckType.Value);

            if (request.Status.HasValue)
                query = query.Where(i => i.Status == request.Status.Value);
            else
                // Varsayılan: AutoResolved'ları gizle (gürültüyü azalt)
                query = query.Where(i => i.Status != ReconciliationStatus.AutoResolved);

            if (request.Severity.HasValue)
                query = query.Where(i => i.Severity == request.Severity.Value);

            if (request.FromDate.HasValue)
                query = query.Where(i => i.DetectedAt >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                query = query.Where(i => i.DetectedAt <= request.ToDate.Value.AddDays(1));

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(i => i.Severity)
                .ThenByDescending(i => i.DetectedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(i => new ReconciliationIssueDto
                {
                    Id                = i.Id,
                    CheckType         = i.CheckType,
                    CheckTypeName     = i.CheckType.ToString(),
                    Severity          = i.Severity,
                    Status            = i.Status,
                    ShipmentId        = i.ShipmentId,
                    ShipmentLineId    = i.ShipmentLineId,
                    IssOrderLineId    = i.IssOrderLineId,
                    Description       = i.Description,
                    ExpectedValue     = i.ExpectedValue,
                    ActualValue       = i.ActualValue,
                    DetectedAt        = i.DetectedAt,
                    AcknowledgedAt    = i.AcknowledgedAt,
                    AcknowledgementNote = i.AcknowledgementNote,
                })
                .ToListAsync(ct);

            // Open özeti — filtreden bağımsız, genel durum tablosu için
            var openSummary = await _context.ReconciliationIssues
                .Where(i => i.Status == ReconciliationStatus.Open)
                .GroupBy(i => i.CheckType)
                .Select(g => new { CheckType = g.Key.ToString(), Count = g.Count() })
                .ToListAsync(ct);

            return new ReconciliationIssuePageDto
            {
                TotalCount  = totalCount,
                Page        = request.Page,
                PageSize    = request.PageSize,
                Items       = items,
                OpenSummary = openSummary.ToDictionary(x => x.CheckType, x => x.Count),
            };
        }
    }
}
