using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Netsis.Queries.GetReconciliation
{
    public record GetReconciliationQuery(
        DateTime? FromDate,
        DateTime? ToDate,
        bool OnlyDiff,
        int? OperationTypeFilter   // 0 = Catering, 1 = Clothing, null = Tümü
    ) : IRequest<List<ShipmentReconciliationDto>>;

    // ── DTOs ─────────────────────────────────────────────────────────────────────

    public enum LineReconciliationStatus
    {
        Equal,
        Revised,
        UnderPicked,
        OverPicked,
        NotPicked,
        ClothingDirect,
    }

    public class LineReconciliationDto
    {
        public int    ShipmentLineId { get; set; }
        public string StockCode      { get; set; } = string.Empty;
        public string StockName      { get; set; } = string.Empty;
        public string Unit           { get; set; } = string.Empty;
        public decimal IssQty        { get; set; }
        public decimal OrderedQty    { get; set; }
        public decimal DeliveredQty  { get; set; }
        public decimal NetsisQty     { get; set; }
        public string  Status        { get; set; } = string.Empty;
    }

    public class ShipmentReconciliationDto
    {
        public int      ShipmentId     { get; set; }
        public string   ProjectName    { get; set; } = string.Empty;
        public DateTime DeliveryDate   { get; set; }
        public string   OperationType  { get; set; } = string.Empty;
        public string?  NetsisOrderNo  { get; set; }
        public string?  IrsaliyeNo     { get; set; }
        public bool     HasDifference  { get; set; }
        public List<LineReconciliationDto> Lines { get; set; } = new();
    }

    // ── Handler ──────────────────────────────────────────────────────────────────

    public class GetReconciliationQueryHandler
        : IRequestHandler<GetReconciliationQuery, List<ShipmentReconciliationDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetReconciliationQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ShipmentReconciliationDto>> Handle(
            GetReconciliationQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Shipments
                .Include(s => s.Project)
                .Include(s => s.IssOrder)
                .Include(s => s.Lines)
                    .ThenInclude(l => l.IssOrderLine)
                .Where(s => s.NetsisTransferredAt.HasValue)
                .AsQueryable();

            if (request.FromDate.HasValue)
                query = query.Where(s => s.DeliveryDate >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                query = query.Where(s => s.DeliveryDate <= request.ToDate.Value);

            if (request.OperationTypeFilter.HasValue)
            {
                var opType = (OperationType)request.OperationTypeFilter.Value;
                query = query.Where(s => s.OperationType == opType);
            }

            var shipments = await query
                .OrderByDescending(s => s.DeliveryDate)
                .ToListAsync(cancellationToken);

            var result = new List<ShipmentReconciliationDto>();

            foreach (var shipment in shipments)
            {
                var isClothing = shipment.OperationType == OperationType.Clothing;

                var lineDtos = shipment.Lines.Select(l =>
                {
                    var issQty  = l.IssOrderLine?.OrderedQty ?? l.OrderedQty;
                    var netsisQty = isClothing
                        ? (l.IssOrderLine?.OrderedQty ?? l.OrderedQty)
                        : l.DeliveredQty;

                    var status = ComputeLineStatus(l.OrderedQty, l.DeliveredQty, issQty, isClothing);

                    return new LineReconciliationDto
                    {
                        ShipmentLineId = l.Id,
                        StockCode      = l.StockCode,
                        StockName      = l.StockName,
                        Unit           = l.Unit.ToString(),
                        IssQty         = issQty,
                        OrderedQty     = l.OrderedQty,
                        DeliveredQty   = l.DeliveredQty,
                        NetsisQty      = netsisQty,
                        Status         = status.ToString(),
                    };
                }).ToList();

                var hasDiff = lineDtos.Any(l =>
                    l.Status != LineReconciliationStatus.Equal.ToString() &&
                    l.Status != LineReconciliationStatus.ClothingDirect.ToString());

                if (request.OnlyDiff && !hasDiff)
                    continue;

                result.Add(new ShipmentReconciliationDto
                {
                    ShipmentId    = shipment.Id,
                    ProjectName   = shipment.Project.Name,
                    DeliveryDate  = shipment.DeliveryDate,
                    OperationType = isClothing ? "Kıyafet" : "Catering",
                    NetsisOrderNo = shipment.IssOrder?.NetsisOrderNumber,
                    IrsaliyeNo    = shipment.IrsaliyeNo,
                    HasDifference = hasDiff,
                    Lines         = lineDtos,
                });
            }

            return result;
        }

        /// <summary>
        /// Satır uzlaştırma durumunu hesaplar.
        /// Öncelik sırası: NotPicked > Revised > UnderPicked > OverPicked > Equal > ClothingDirect
        /// </summary>
        private static LineReconciliationStatus ComputeLineStatus(
            decimal orderedQty, decimal deliveredQty, decimal issQty, bool isClothing)
        {
            if (isClothing)
                return LineReconciliationStatus.ClothingDirect;

            if (deliveredQty == 0)
                return LineReconciliationStatus.NotPicked;

            if (issQty != orderedQty)
                return LineReconciliationStatus.Revised;

            if (deliveredQty < orderedQty)
                return LineReconciliationStatus.UnderPicked;

            if (deliveredQty > orderedQty)
                return LineReconciliationStatus.OverPicked;

            return LineReconciliationStatus.Equal;
        }
    }
}
