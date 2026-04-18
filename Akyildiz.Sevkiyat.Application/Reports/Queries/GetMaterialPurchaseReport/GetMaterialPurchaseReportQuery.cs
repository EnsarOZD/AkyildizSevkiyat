using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Reports.Queries.GetMaterialPurchaseReport
{
    public record GetMaterialPurchaseReportQuery : IRequest<List<MaterialPurchaseReportRow>>
    {
        public Guid? SupplierId { get; set; }
        public int? StockMasterId { get; set; }
        public string? MaterialName { get; set; }
    }

    public class MaterialPurchaseReportRow
    {
        public Guid SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public int StockMasterId { get; set; }
        public string StockCode { get; set; } = string.Empty;
        public string StockName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal OrderedQty { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal RemainingQty { get; set; }
    }

    public class GetMaterialPurchaseReportQueryHandler : IRequestHandler<GetMaterialPurchaseReportQuery, List<MaterialPurchaseReportRow>>
    {
        private readonly IApplicationDbContext _context;

        public GetMaterialPurchaseReportQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<MaterialPurchaseReportRow>> Handle(GetMaterialPurchaseReportQuery request, CancellationToken cancellationToken)
        {
            var q = _context.PurchaseOrderLines
                .Include(pol => pol.PurchaseOrder)
                .Include(pol => pol.StockMaster)
                .Where(pol => pol.PurchaseOrder.Status != PurchaseOrderStatus.Cancelled && pol.PurchaseOrder.Status != PurchaseOrderStatus.Draft);

            if (request.SupplierId.HasValue)
                q = q.Where(pol => pol.PurchaseOrder.SupplierId == request.SupplierId.Value);

            if (request.StockMasterId.HasValue)
                q = q.Where(pol => pol.StockMasterId == request.StockMasterId.Value);

            if (!string.IsNullOrWhiteSpace(request.MaterialName))
            {
                var search = request.MaterialName.Trim().ToLower();
                q = q.Where(pol => pol.StockMaster.StockName.ToLower().Contains(search) || pol.StockMaster.StockCode.ToLower().Contains(search));
            }

            var poLines = await q.ToListAsync(cancellationToken);

            var poLineIds = poLines.Select(x => x.Id).ToList();

            var receivedSums = await _context.GoodsReceiptLines
                .Where(grl => grl.PurchaseOrderLineId.HasValue 
                           && poLineIds.Contains(grl.PurchaseOrderLineId.Value) 
                           && grl.GoodsReceipt.Status == GoodsReceiptStatus.Posted)
                .GroupBy(grl => grl.PurchaseOrderLineId!.Value)
                .Select(g => new { PoLineId = g.Key, TotalReceived = g.Sum(x => x.AcceptedQty ?? x.ReceivedQty) })
                .ToDictionaryAsync(k => k.PoLineId, v => v.TotalReceived, cancellationToken);

            var grouped = poLines
                .GroupBy(pol => new { 
                    pol.PurchaseOrder.SupplierId, 
                    pol.PurchaseOrder.SupplierNameSnapshot, 
                    pol.StockMasterId, 
                    StockCode = pol.StockMaster != null ? pol.StockMaster.StockCode : "",
                    StockName = pol.StockMaster != null ? pol.StockMaster.StockName : "",
                    Unit = pol.Unit.ToString()
                })
                .Select(g => new MaterialPurchaseReportRow
                {
                    SupplierId = g.Key.SupplierId,
                    SupplierName = g.Key.SupplierNameSnapshot,
                    StockMasterId = g.Key.StockMasterId,
                    StockCode = g.Key.StockCode,
                    StockName = g.Key.StockName,
                    Unit = g.Key.Unit,
                    OrderedQty = g.Sum(x => x.OrderedQty),
                    ReceivedQty = g.Sum(x => poLines.Where(pol => pol.PurchaseOrder.SupplierId == g.Key.SupplierId && pol.StockMasterId == g.Key.StockMasterId).Select(pol => receivedSums.ContainsKey(pol.Id) ? receivedSums[pol.Id] : 0).Sum())
                }).ToList();

            // Correct ReceivedQty logic - it should sum the received quantities for all lines that fell into this group
            // The previous line was a bit complex, let's simplify by calculating first.
            
            // Re-calculating correctly:
            var result = poLines
                .GroupBy(pol => new { 
                    pol.PurchaseOrder.SupplierId, 
                    pol.PurchaseOrder.SupplierNameSnapshot, 
                    pol.StockMasterId, 
                    StockCode = pol.StockMaster != null ? pol.StockMaster.StockCode : "",
                    StockName = pol.StockMaster != null ? pol.StockMaster.StockName : "",
                    Unit = pol.Unit.ToString()
                })
                .Select(g => {
                    var ordered = g.Sum(x => x.OrderedQty);
                    var received = g.Sum(x => receivedSums.ContainsKey(x.Id) ? receivedSums[x.Id] : 0);
                    return new MaterialPurchaseReportRow
                    {
                        SupplierId = g.Key.SupplierId,
                        SupplierName = g.Key.SupplierNameSnapshot,
                        StockMasterId = g.Key.StockMasterId,
                        StockCode = g.Key.StockCode,
                        StockName = g.Key.StockName,
                        Unit = g.Key.Unit,
                        OrderedQty = ordered,
                        ReceivedQty = received,
                        RemainingQty = (ordered - received) < 0 ? 0 : (ordered - received)
                    };
                })
                .OrderBy(x => x.SupplierName)
                .ThenBy(x => x.StockName)
                .ToList();

            return result;
        }
    }
}
