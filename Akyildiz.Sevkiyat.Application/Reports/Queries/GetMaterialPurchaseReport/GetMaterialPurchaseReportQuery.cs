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
    }

    public class MaterialPurchaseReportRow
    {
        public Guid SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public int StockMasterId { get; set; }
        public string StockName { get; set; } = string.Empty;
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
                .Where(pol => pol.PurchaseOrder.Status != PurchaseOrderStatus.Cancelled && pol.PurchaseOrder.Status != PurchaseOrderStatus.Draft);

            if (request.SupplierId.HasValue)
                q = q.Where(pol => pol.PurchaseOrder.SupplierId == request.SupplierId.Value);

            if (request.StockMasterId.HasValue)
                q = q.Where(pol => pol.StockMasterId == request.StockMasterId.Value);

            var grouped = await q
                .GroupBy(pol => new { 
                    pol.PurchaseOrder.SupplierId, 
                    pol.PurchaseOrder.SupplierNameSnapshot, 
                    pol.StockMasterId, 
                    StockNameSnapshot = pol.StockMaster != null ? pol.StockMaster.StockName : pol.StockNameSnapshot 
                })
                .Select(g => new MaterialPurchaseReportRow
                {
                    SupplierId = g.Key.SupplierId,
                    SupplierName = g.Key.SupplierNameSnapshot,
                    StockMasterId = g.Key.StockMasterId,
                    StockName = g.Key.StockNameSnapshot ?? "",
                    OrderedQty = g.Sum(x => x.OrderedQty),
                    ReceivedQty = g.Sum(x => x.ReceivedQty)
                })
                .ToListAsync(cancellationToken);

            foreach (var row in grouped)
            {
                row.RemainingQty = row.OrderedQty - row.ReceivedQty;
                if (row.RemainingQty < 0) row.RemainingQty = 0;
            }

            return grouped.OrderBy(x => x.SupplierName).ThenBy(x => x.StockName).ToList();
        }
    }
}
