using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.GoodsReceipts.Queries.GetGoodsReceipts
{
    public class GetGoodsReceiptsQuery : IRequest<List<GoodsReceiptDto>>
    {
        public GoodsReceiptStatus? Status { get; set; }
        public string? SupplierName { get; set; }
        public DateOnly? StartData { get; set; }
        public DateOnly? EndDate { get; set; }
    }

    public class GoodsReceiptDto
    {
        public Guid Id { get; set; }
        public Guid? PurchaseOrderId { get; set; }
        public string? PurchaseOrderNumber { get; set; }
        // Çok-PO'lu mal kabullerde başlık PO'su NULL olur; sipariş no'ları satırlardan türetilir.
        public List<string> PurchaseOrderNumbers { get; set; } = new();
        public Guid SupplierId { get; set; }
        public string SupplierNameSnapshot { get; set; } = string.Empty;
        public DateOnly ReceiptDate { get; set; }
        public string WaybillNo { get; set; } = string.Empty;
        public DateOnly WaybillDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int StatusValue { get; set; }
        public string? Note { get; set; }
        public int LineCount { get; set; }
    }

    public class GetGoodsReceiptsQueryHandler : IRequestHandler<GetGoodsReceiptsQuery, List<GoodsReceiptDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetGoodsReceiptsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<GoodsReceiptDto>> Handle(GetGoodsReceiptsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.GoodsReceipts.AsQueryable();

            if (request.Status.HasValue)
            {
                query = query.Where(x => x.Status == request.Status.Value);
            }

            if (!string.IsNullOrEmpty(request.SupplierName))
            {
                query = query.Where(x => EF.Functions.Collate(x.SupplierNameSnapshot, "Turkish_CI_AS").Contains(EF.Functions.Collate(request.SupplierName, "Turkish_CI_AS")));
            }

            if (request.StartData.HasValue)
            {
                query = query.Where(x => x.ReceiptDate >= request.StartData.Value);
            }

            if (request.EndDate.HasValue)
            {
                query = query.Where(x => x.ReceiptDate <= request.EndDate.Value);
            }

            var list = await query
                .Include(x => x.PurchaseOrder)
                .OrderByDescending(x => x.ReceiptDate)
                .Select(x => new GoodsReceiptDto
                {
                    Id = x.Id,
                    PurchaseOrderId = x.PurchaseOrderId,
                    PurchaseOrderNumber = x.PurchaseOrder != null ? x.PurchaseOrder.OrderNumber : null,
                    SupplierId = x.SupplierId,
                    SupplierNameSnapshot = x.SupplierNameSnapshot,
                    ReceiptDate = x.ReceiptDate,
                    WaybillNo = x.WaybillNo,
                    WaybillDate = x.WaybillDate,
                    Status = x.Status.ToString(),
                    StatusValue = (int)x.Status,
                    Note = x.Note,
                    LineCount = x.Lines.Count
                })
                .ToListAsync(cancellationToken);

            // Her mal kabul için satırlardan türetilmiş benzersiz sipariş no'ları
            // (tekli PO + çok-PO'lu mal kabuller dahil) — liste ekranında gösterilir.
            var receiptIds = list.Select(r => r.Id).ToList();
            if (receiptIds.Count > 0)
            {
                var poNumbersByReceipt = await (
                        from grl in _context.GoodsReceiptLines
                        join pol in _context.PurchaseOrderLines on grl.PurchaseOrderLineId equals pol.Id
                        join po in _context.PurchaseOrders on pol.PurchaseOrderId equals po.Id
                        where receiptIds.Contains(grl.GoodsReceiptId)
                        select new { grl.GoodsReceiptId, po.OrderNumber })
                    .Distinct()
                    .ToListAsync(cancellationToken);

                var byReceipt = poNumbersByReceipt
                    .GroupBy(x => x.GoodsReceiptId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => x.OrderNumber).Distinct().OrderBy(s => s).ToList());

                foreach (var r in list)
                    if (byReceipt.TryGetValue(r.Id, out var nums))
                        r.PurchaseOrderNumbers = nums;
            }

            return list;
        }
    }
}
