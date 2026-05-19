namespace Akyildiz.Sevkiyat.Application.PurchaseOrders.Queries.GetReceivableMaterials
{
    public class GetReceivableMaterialsQuery : IRequest<List<ReceivableMaterialDto>>
    {
        public string? SearchTerm { get; set; }
    }

    public class ReceivableMaterialDto
    {
        public int StockMasterId { get; set; }
        public string StockName { get; set; } = string.Empty;
        public string StockCode { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal TotalRemainingQty { get; set; }
        public int AllocationCount { get; set; }
        public List<MaterialPoAllocationDto> Allocations { get; set; } = new();
    }

    public class MaterialPoAllocationDto
    {
        public Guid PurchaseOrderLineId { get; set; }
        public Guid PurchaseOrderId { get; set; }
        public string PurchaseOrderNumber { get; set; } = string.Empty;
        public string SupplierNameSnapshot { get; set; } = string.Empty;
        public Guid SupplierId { get; set; }
        public decimal OrderedQty { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal RemainingQty { get; set; }
    }

    public class GetReceivableMaterialsQueryHandler : IRequestHandler<GetReceivableMaterialsQuery, List<ReceivableMaterialDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetReceivableMaterialsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ReceivableMaterialDto>> Handle(GetReceivableMaterialsQuery request, CancellationToken cancellationToken)
        {
            var rawLines = await _context.PurchaseOrderLines
                .Where(pol => pol.PurchaseOrder.Status == PurchaseOrderStatus.Approved ||
                              pol.PurchaseOrder.Status == PurchaseOrderStatus.PartiallyReceived)
                .Select(pol => new
                {
                    pol.Id,
                    pol.PurchaseOrderId,
                    pol.PurchaseOrder.OrderNumber,
                    pol.PurchaseOrder.SupplierNameSnapshot,
                    pol.PurchaseOrder.SupplierId,
                    pol.StockMasterId,
                    pol.StockMaster.StockName,
                    pol.StockMaster.StockCode,
                    pol.Unit,
                    pol.OrderedQty,
                    ReceivedQty = _context.GoodsReceiptLines
                        .Where(grl => grl.PurchaseOrderLineId == pol.Id &&
                                      grl.GoodsReceipt.Status == GoodsReceiptStatus.Posted)
                        .Sum(grl => (decimal?)grl.AcceptedQty) ?? 0
                })
                .ToListAsync(cancellationToken);

            var searchTerm = request.SearchTerm?.Trim().ToUpperInvariant();

            var linesWithRemaining = rawLines
                .Select(l => new { Line = l, RemainingQty = Math.Max(0, l.OrderedQty - l.ReceivedQty) })
                .Where(x => x.RemainingQty > 0)
                .ToList();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                linesWithRemaining = linesWithRemaining
                    .Where(x => x.Line.StockName.ToUpperInvariant().Contains(searchTerm) ||
                                x.Line.StockCode.ToUpperInvariant().Contains(searchTerm))
                    .ToList();
            }

            return linesWithRemaining
                .GroupBy(x => x.Line.StockMasterId)
                .Select(g => new ReceivableMaterialDto
                {
                    StockMasterId = g.Key,
                    StockName = g.First().Line.StockName,
                    StockCode = g.First().Line.StockCode,
                    Unit = g.First().Line.Unit.ToString(),
                    TotalRemainingQty = g.Sum(x => x.RemainingQty),
                    AllocationCount = g.Count(),
                    Allocations = g.Select(x => new MaterialPoAllocationDto
                    {
                        PurchaseOrderLineId = x.Line.Id,
                        PurchaseOrderId = x.Line.PurchaseOrderId,
                        PurchaseOrderNumber = x.Line.OrderNumber,
                        SupplierNameSnapshot = x.Line.SupplierNameSnapshot,
                        SupplierId = x.Line.SupplierId,
                        OrderedQty = x.Line.OrderedQty,
                        ReceivedQty = x.Line.ReceivedQty,
                        RemainingQty = x.RemainingQty
                    }).ToList()
                })
                .OrderBy(x => x.StockName)
                .ToList();
        }
    }
}
