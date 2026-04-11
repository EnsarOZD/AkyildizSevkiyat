using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Application.PurchaseOrders.Queries.GetPurchaseOrderDetail
{
    public class GetPurchaseOrderDetailQuery : IRequest<PurchaseOrderDetailDto>
    {
        public Guid Id { get; set; }
    }

    public class PurchaseOrderDetailDto
    {
        public Guid Id { get; set; }
        public Guid SupplierId { get; set; }
        public string SupplierNameSnapshot { get; set; } = string.Empty;
        public string? SupplierEmail { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateOnly OrderDate { get; set; }
        public DateOnly? ExpectedDeliveryDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int StatusValue { get; set; }
        public string? Note { get; set; }
        public string? ExternalRef { get; set; }
        public DateTime? NetsisTransferredAt { get; set; }
        public bool IsEditable { get; set; }
        public string? DepotName { get; set; }
        public string? DepotAddress { get; set; }
        public List<PurchaseOrderLineDetailDto> Lines { get; set; } = new();
    }

    public class PurchaseOrderLineDetailDto
    {
        public Guid Id { get; set; }
        public int StockMasterId { get; set; }
        public string StockCode { get; set; } = string.Empty;
        public string StockName { get; set; } = string.Empty;
        public decimal OrderedQty { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal? UnitPrice { get; set; }
        public decimal? TotalPrice => UnitPrice.HasValue ? UnitPrice.Value * OrderedQty : null;

        // Computed
        public decimal ReceivedQty { get; set; }
        public decimal RemainingQty { get; set; }
        public decimal RejectedQty { get; set; }

        public string? Note { get; set; }
    }

    public class GetPurchaseOrderDetailQueryHandler : IRequestHandler<GetPurchaseOrderDetailQuery, PurchaseOrderDetailDto>
    {
        private readonly IApplicationDbContext _context;

        public GetPurchaseOrderDetailQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PurchaseOrderDetailDto> Handle(GetPurchaseOrderDetailQuery request, CancellationToken cancellationToken)
        {
            var po = await _context.PurchaseOrders
                .Include(p => p.Lines)
                .ThenInclude(l => l.StockMaster)
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
            
            if (po == null) throw new NotFoundException("PurchaseOrder", request.Id);

            // Calculate Received Qty from GoodsReceiptLines
            // We need to fetch all valid (POSTED only) GoodsReceiptLines linked to these PO Lines.
            
            var lineIds = po.Lines.Select(l => l.Id).ToList();
            
            var receiptLines = await _context.GoodsReceiptLines
                .Include(grl => grl.GoodsReceipt)
                .Where(grl => grl.GoodsReceipt.Status == GoodsReceiptStatus.Posted // Strict: Only Posted counts
                              && grl.PurchaseOrderLineId.HasValue 
                              && lineIds.Contains(grl.PurchaseOrderLineId.Value))
                .Select(grl => new { grl.PurchaseOrderLineId, grl.ReceivedQty, grl.RejectedQty })
                .ToListAsync(cancellationToken);

            var depot = await _context.SystemSettings.FindAsync(new object[] { 1 }, cancellationToken);

            var dto = new PurchaseOrderDetailDto
            {
                Id = po.Id,
                SupplierId = po.SupplierId,
                SupplierNameSnapshot = po.SupplierNameSnapshot,
                SupplierEmail = po.Supplier?.Email,
                OrderNumber = po.OrderNumber,
                OrderDate = po.OrderDate,
                ExpectedDeliveryDate = po.ExpectedDeliveryDate,
                Status = po.Status.ToString(),
                StatusValue = (int)po.Status,
                Note = po.Note,
                ExternalRef = po.ExternalRef,
                NetsisTransferredAt = po.NetsisTransferredAt,
                IsEditable = po.IsEditable,
                DepotName = depot?.DepotName,
                DepotAddress = depot?.DepotAddress,
                Lines = po.Lines.Select(l =>
                {
                    var received = receiptLines
                        .Where(r => r.PurchaseOrderLineId == l.Id)
                        .Sum(r => r.ReceivedQty);

                    var rejected = receiptLines
                         .Where(r => r.PurchaseOrderLineId == l.Id)
                         .Sum(r => r.RejectedQty ?? 0);

                    return new PurchaseOrderLineDetailDto
                    {
                        Id = l.Id,
                        StockMasterId = l.StockMasterId,
                        StockCode = l.StockMaster?.StockCode ?? "N/A",
                        StockName = l.StockMaster?.StockName ?? "Silinmiş Ürün",
                        OrderedQty = l.OrderedQty,
                        Unit = l.Unit.ToString(),
                        UnitPrice = l.UnitPrice,
                        Note = l.Note,
                        ReceivedQty = received,
                        RejectedQty = rejected,
                        RemainingQty = Math.Max(0, l.OrderedQty - received)
                    };
                }).ToList()
            };

            return dto;
        }
    }
}
