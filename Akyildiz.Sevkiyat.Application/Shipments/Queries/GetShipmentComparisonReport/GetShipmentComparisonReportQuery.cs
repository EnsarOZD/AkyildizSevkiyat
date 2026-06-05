using MediatR;
using Microsoft.EntityFrameworkCore;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;

namespace Akyildiz.Sevkiyat.Application.Shipments.Queries.GetShipmentComparisonReport
{
    public record GetShipmentComparisonReportQuery(
        DateOnly? DateFrom = null,
        DateOnly? DateTo = null,
        int? ProjectId = null,
        int? ZoneId = null,
        string? StatusFilter = null,   // "all" | "issues" | "missing"
        bool? MailSentFilter = null,   // true = mail gönderildi, false = gönderilmedi, null = tümü
        int PageNumber = 1,
        int PageSize = 25
    ) : IRequest<ShipmentComparisonReportDto>;

    // ── DTOs ─────────────────────────────────────────────────────────────────

    public class ShipmentComparisonReportDto
    {
        public int TotalCount { get; init; }
        public int PageIndex { get; init; }
        public int TotalPages { get; init; }
        public int TotalIssues { get; init; }   // shipments with any problem
        public int TotalMissing { get; init; }   // shipments with completely missing lines
        public IReadOnlyList<ShipmentComparisonDto> Items { get; init; } = [];
    }

    public class ShipmentComparisonDto
    {
        public int ShipmentId { get; init; }
        public string? OrderNumber { get; init; }
        public string? TalepNo { get; init; }
        public string? IrsaliyeNo { get; init; }
        public string? YoneticiMail { get; init; }
        public DateTime? MissingItemsMailSentAt { get; init; }
        public string ProjectCode { get; init; } = string.Empty;
        public string ProjectName { get; init; } = string.Empty;
        public string? ZoneName { get; init; }
        public DateTime DeliveryDate { get; init; }
        public string ShipmentStatus { get; init; } = string.Empty;
        public string? CancelReason { get; init; }
        public string OverallStatus { get; init; } = string.Empty;
        // "full_match" | "has_substitutions" | "has_shortfalls" | "has_missing" | "critical"
        public IReadOnlyList<LineComparisonDto> Lines { get; init; } = [];
    }

    public class LineComparisonDto
    {
        // ISS side
        public string IssStockCode { get; init; } = string.Empty;
        public string IssStockName { get; init; } = string.Empty;
        public decimal IssOrderedQty { get; init; }

        // Actual side (null = not in shipment at all)
        public string? ActualStockCode { get; init; }
        public string? ActualStockName { get; init; }
        public decimal ActualQty { get; init; }

        // "full_match" | "partial" | "substitution" | "partial_substitution" | "no_fulfillment" | "missing" | "extra"
        public string Status { get; init; } = string.Empty;
        public string? DifferenceReason { get; init; }
    }

    // ── Handler ──────────────────────────────────────────────────────────────

    public class GetShipmentComparisonReportQueryHandler
        : IRequestHandler<GetShipmentComparisonReportQuery, ShipmentComparisonReportDto>
    {
        private readonly IApplicationDbContext _context;

        public GetShipmentComparisonReportQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ShipmentComparisonReportDto> Handle(
            GetShipmentComparisonReportQuery request,
            CancellationToken cancellationToken)
        {
            var dateFrom = request.DateFrom ?? DateOnly.FromDateTime(DateTime.Today);
            var dateTo   = request.DateTo   ?? dateFrom;

            var fromDt = dateFrom.ToDateTime(TimeOnly.MinValue);
            var toDt   = dateTo.ToDateTime(TimeOnly.MaxValue);

            // ReadyForDispatch ve sonrası gösterilir. Cancelled tamamen hariç tutulur.
            // Passive sevkiyatlar normalde gizlidir; ANCAK sebep girilerek iptal edilenler
            // (CancelReason dolu) raporda görünür ki depo karşılayamadığı siparişler için
            // müşteriye bildirim e-postası gönderilebilsin.
            var query = _context.Shipments
                .Include(s => s.Project)
                    .ThenInclude(p => p.Zone)
                .Include(s => s.IssOrder)
                    .ThenInclude(o => o!.Lines)
                .Include(s => s.Lines)
                    .ThenInclude(l => l.IssOrderLine)
                .Where(s => s.DeliveryDate >= fromDt
                         && s.DeliveryDate <= toDt
                         && s.Status >= ShipmentStatus.ReadyForDispatch
                         && s.Status != ShipmentStatus.Cancelled
                         && (s.Status != ShipmentStatus.Passive || s.CancelReason != null));

            if (request.ProjectId.HasValue)
                query = query.Where(s => s.ProjectId == request.ProjectId.Value);

            if (request.ZoneId.HasValue)
                query = query.Where(s => s.Project.ZoneId == request.ZoneId.Value);

            if (request.MailSentFilter.HasValue)
                query = request.MailSentFilter.Value
                    ? query.Where(s => s.MissingItemsMailSentAt.HasValue)
                    : query.Where(s => !s.MissingItemsMailSentAt.HasValue);

            var shipments = await query
                .OrderBy(s => s.DeliveryDate)
                .ThenBy(s => s.Project.Name)
                .ToListAsync(cancellationToken);

            // Load StockMappings for all ISS codes in this batch (both from ISS
            // order lines AND shipment lines) so we can distinguish a true
            // substitution from a normal mapping.
            var issStockCodes = shipments
                .SelectMany(s => s.IssOrder?.Lines ?? [])
                .Select(l => l.StockCode)
                .Concat(shipments.SelectMany(s => s.Lines).Select(l => l.StockCode))
                .Where(c => c != null)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var mappingsList = await _context.StockMappings
                .Include(m => m.LocalStock)
                .Where(m => issStockCodes.Contains(m.ExternalStockCode))
                .ToListAsync(cancellationToken);

            // ISS external code → internal StockMaster.StockCode
            var issToInternalMap = mappingsList
                .GroupBy(m => m.ExternalStockCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.First().LocalStock?.StockCode,
                    StringComparer.OrdinalIgnoreCase);

            // Build comparison DTOs
            var comparisons = shipments
                .Select(s => BuildComparison(s, issToInternalMap))
                .ToList();

            // Apply status filter AFTER in-memory build (needs computed OverallStatus)
            if (request.StatusFilter == "issues")
                comparisons = comparisons.Where(c => c.OverallStatus != "full_match").ToList();
            else if (request.StatusFilter == "missing")
                comparisons = comparisons.Where(c => c.OverallStatus is "has_missing" or "critical").ToList();

            int totalCount  = comparisons.Count;
            int totalIssues = comparisons.Count(c => c.OverallStatus != "full_match");
            int totalMissing = comparisons.Count(c => c.OverallStatus is "has_missing" or "critical");
            int totalPages  = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            var paged = comparisons
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new ShipmentComparisonReportDto
            {
                TotalCount   = totalCount,
                PageIndex    = request.PageNumber,
                TotalPages   = totalPages,
                TotalIssues  = totalIssues,
                TotalMissing = totalMissing,
                Items        = paged,
            };
        }

        private static string? StripAkiPrefix(string? value) =>
            value?.StartsWith("AKI", StringComparison.OrdinalIgnoreCase) == true
                ? value[3..]
                : value;

        // Normalizes a stock code to a comparable internal code:
        // - If the code is an ISS code with a known mapping → returns the internal StockMaster code
        // - Otherwise → returns the raw code (could be an internal code already, or an unmapped ISS code)
        private static string? ResolveToComparableCode(
            string? rawCode,
            IReadOnlyDictionary<string, string?> issToInternalMap)
        {
            if (rawCode == null) return null;
            var trimmed = rawCode.Trim();
            if (issToInternalMap.TryGetValue(trimmed, out var mapped) && mapped != null)
                return mapped.Trim();
            return trimmed;
        }

        private static ShipmentComparisonDto BuildComparison(
            Domain.Entities.Shipment shipment,
            IReadOnlyDictionary<string, string?> issToInternalMap)
        {
            var issLines      = shipment.IssOrder?.Lines?.ToList() ?? [];
            var shipmentLines = shipment.Lines.ToList();

            // Map IssOrderLineId → ShipmentLine
            var linkedMap = shipmentLines
                .Where(l => l.IssOrderLineId.HasValue)
                .ToDictionary(l => l.IssOrderLineId!.Value);

            var lineResults = new List<LineComparisonDto>();

            // 1. For every ISS line, find the matching shipment line
            foreach (var issLine in issLines)
            {
                if (linkedMap.TryGetValue(issLine.Id, out var sl))
                {
                    // Normalize both sides to internal codes before comparing.
                    // This handles: same ISS code, ISS→internal mapping, or
                    // shipment lines that already carry the internal code directly.
                    var expectedNorm = ResolveToComparableCode(issLine.StockCode, issToInternalMap);
                    var actualNorm   = ResolveToComparableCode(sl.StockCode, issToInternalMap);
                    bool isSubstitution = !string.Equals(expectedNorm, actualNorm,
                                                         StringComparison.OrdinalIgnoreCase);
                    bool isZero         = sl.DeliveredQty == 0;
                    bool isPartial      = !isZero && sl.DeliveredQty < issLine.OrderedQty;

                    string status = (isSubstitution, isZero, isPartial) switch
                    {
                        (_, true, _)      => "no_fulfillment",
                        (true, _, true)   => "partial_substitution",
                        (true, _, _)      => "substitution",
                        (_, _, true)      => "partial",
                        _                 => "full_match",
                    };

                    lineResults.Add(new LineComparisonDto
                    {
                        IssStockCode    = issLine.StockCode,
                        IssStockName    = issLine.StockName,
                        IssOrderedQty   = issLine.OrderedQty,
                        ActualStockCode = sl.StockCode,
                        ActualStockName = sl.StockName,
                        ActualQty       = sl.DeliveredQty,
                        Status          = status,
                        DifferenceReason = sl.DifferenceReason,
                    });
                }
                else
                {
                    lineResults.Add(new LineComparisonDto
                    {
                        IssStockCode  = issLine.StockCode,
                        IssStockName  = issLine.StockName,
                        IssOrderedQty = issLine.OrderedQty,
                        ActualQty     = 0,
                        Status        = "missing",
                    });
                }
            }

            // 2. Shipment lines with no ISS link (manually added / unlinked)
            var extraLines = shipmentLines.Where(l => !l.IssOrderLineId.HasValue);
            foreach (var el in extraLines)
            {
                lineResults.Add(new LineComparisonDto
                {
                    IssStockCode    = el.StockCode,
                    IssStockName    = el.StockName,
                    IssOrderedQty   = 0,
                    ActualStockCode = el.StockCode,
                    ActualStockName = el.StockName,
                    ActualQty       = el.DeliveredQty,
                    Status          = "extra",
                });
            }

            // 3. Compute overall status
            bool hasMissing       = lineResults.Any(l => l.Status is "missing" or "no_fulfillment");
            bool hasSubstitutions = lineResults.Any(l => l.Status is "substitution" or "partial_substitution");
            bool hasShortfalls    = lineResults.Any(l => l.Status is "partial" or "partial_substitution");

            string overallStatus = (hasMissing, hasSubstitutions || hasShortfalls) switch
            {
                (true, true)  => "critical",
                (true, false) => "has_missing",
                (false, true) => hasSubstitutions ? "has_substitutions" : "has_shortfalls",
                _             => "full_match",
            };

            return new ShipmentComparisonDto
            {
                ShipmentId    = shipment.Id,
                OrderNumber   = StripAkiPrefix(shipment.IssOrder?.ExternalOrderNumber),
                TalepNo       = shipment.TalepNo ?? shipment.IssOrder?.TalepNo,
                YoneticiMail  = shipment.IssOrder?.YoneticiMailAdresleri,
                MissingItemsMailSentAt = shipment.MissingItemsMailSentAt,
                IrsaliyeNo     = shipment.IrsaliyeNo,
                ProjectCode    = shipment.Project.Code,
                ProjectName    = shipment.Project.Name,
                ZoneName       = shipment.Project.Zone?.Name,
                DeliveryDate   = shipment.DeliveryDate,
                ShipmentStatus = shipment.Status.ToString(),
                CancelReason   = shipment.CancelReason,
                OverallStatus  = overallStatus,
                Lines          = lineResults,
            };
        }
    }
}
