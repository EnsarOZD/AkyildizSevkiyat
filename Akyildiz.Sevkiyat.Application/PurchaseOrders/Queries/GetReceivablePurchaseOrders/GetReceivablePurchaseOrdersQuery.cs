using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.PurchaseOrders.Queries.GetReceivablePurchaseOrders
{
    public class GetReceivablePurchaseOrdersQuery : IRequest<List<ReceivablePurchaseOrderDto>>
    {
        public Guid SupplierId { get; set; }
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        public string? SearchTerm { get; set; }
        public int Take { get; set; } = 50;
    }

    public class ReceivablePurchaseOrderDto
    {
        public Guid PurchaseOrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string SupplierNameSnapshot { get; set; } = string.Empty;
        public DateOnly OrderDate { get; set; }
        public decimal ReceivedPercent { get; set; }
        public int RemainingLineCount { get; set; }
        public decimal RemainingTotalQty { get; set; }
        public int MatchingLinesCount { get; set; }
        public bool HasStockCodeMatch { get; set; }
        public List<ReceivablePOLineDto> MatchingLinesPreview { get; set; } = new();
    }

    public class ReceivablePOLineDto
    {
        public Guid PurchaseOrderLineId { get; set; }
        public string StockCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal OrderedQty { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal RemainingQty { get; set; }
    }

    public class GetReceivablePurchaseOrdersQueryHandler : IRequestHandler<GetReceivablePurchaseOrdersQuery, List<ReceivablePurchaseOrderDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetReceivablePurchaseOrdersQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ReceivablePurchaseOrderDto>> Handle(GetReceivablePurchaseOrdersQuery request, CancellationToken cancellationToken)
        {
            if (request.SupplierId == Guid.Empty)
                return new List<ReceivablePurchaseOrderDto>();

            var fromDate = request.FromDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
            var toDate = request.ToDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

            // Base query for POs
            var poQuery = _context.PurchaseOrders
                .Where(x => x.SupplierId == request.SupplierId)
                .Where(x => x.Status == PurchaseOrderStatus.Approved || x.Status == PurchaseOrderStatus.PartiallyReceived)
                .Where(x => x.OrderDate >= fromDate && x.OrderDate <= toDate);

            // Fetch POs with their lines and related POSTED goods receipt lines for calculation
            // We'll use a projection to keep it efficient
            var rawData = await poQuery
                .OrderByDescending(x => x.OrderDate)
                .Take(request.Take)
                .Select(po => new
                {
                    po.Id,
                    po.OrderNumber,
                    po.SupplierNameSnapshot,
                    po.OrderDate,
                    Lines = po.Lines.Select(pol => new
                    {
                        pol.Id,
                        pol.StockMaster.StockCode,
                        pol.StockMaster.StockName,
                        pol.OrderedQty,
                        // Sum received qty from POSTED receipts only
                        ReceivedQty = _context.GoodsReceiptLines
                            .Where(grl => grl.PurchaseOrderLineId == pol.Id && grl.GoodsReceipt.Status == GoodsReceiptStatus.Posted)
                            .Sum(grl => (decimal?)grl.AcceptedQty) ?? 0
                    }).ToList()
                })
                .ToListAsync(cancellationToken);

            var result = new List<ReceivablePurchaseOrderDto>();

            var searchTerm = request.SearchTerm?.Trim().ToUpperInvariant();

            foreach (var po in rawData)
            {
                var processedLines = po.Lines.Select(l => new ReceivablePOLineDto
                {
                    PurchaseOrderLineId = l.Id,
                    StockCode = l.StockCode,
                    ProductName = l.StockName,
                    OrderedQty = l.OrderedQty,
                    ReceivedQty = l.ReceivedQty,
                    RemainingQty = Math.Max(0, l.OrderedQty - l.ReceivedQty)
                }).ToList();

                // Fulfillment Stats
                var totalOrdered = processedLines.Sum(x => x.OrderedQty);
                var totalReceived = processedLines.Sum(x => x.ReceivedQty);
                var remainingLines = processedLines.Where(x => x.RemainingQty > 0).ToList();

                var matchingLines = processedLines;
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    matchingLines = processedLines.Where(l => 
                        l.StockCode.ToUpperInvariant().Contains(searchTerm) || 
                        l.ProductName.ToUpperInvariant().Contains(searchTerm)
                    ).ToList();

                    // If we have a search term and no lines match, skip this PO
                    if (!matchingLines.Any()) continue;
                }

                result.Add(new ReceivablePurchaseOrderDto
                {
                    PurchaseOrderId = po.Id,
                    OrderNumber = po.OrderNumber,
                    SupplierNameSnapshot = po.SupplierNameSnapshot,
                    OrderDate = po.OrderDate,
                    ReceivedPercent = totalOrdered > 0 ? (totalReceived / totalOrdered) * 100 : 0,
                    RemainingLineCount = remainingLines.Count,
                    RemainingTotalQty = remainingLines.Sum(x => x.RemainingQty),
                    MatchingLinesCount = matchingLines.Count,
                    HasStockCodeMatch = !string.IsNullOrEmpty(searchTerm) && matchingLines.Any(l => l.StockCode.ToUpperInvariant().Contains(searchTerm)),
                    MatchingLinesPreview = matchingLines.Take(5).ToList()
                });
            }

            // If searching, order by matching lines count or relevance:
            // 1. POs with matches on StockCode
            // 2. POs with matches on ProductName
            if (!string.IsNullOrEmpty(searchTerm))
            {
                result = result.OrderByDescending(po => po.HasStockCodeMatch ? 2 : 1)
                    .ThenByDescending(x => x.MatchingLinesCount)
                    .ToList();
            }

            return result;
        }
    }
}
