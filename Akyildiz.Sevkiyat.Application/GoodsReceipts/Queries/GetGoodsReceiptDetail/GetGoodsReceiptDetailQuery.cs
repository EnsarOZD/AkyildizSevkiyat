using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Application.GoodsReceipts.Queries.GetGoodsReceiptDetail
{
    public class GetGoodsReceiptDetailQuery : IRequest<GoodsReceiptDetailDto>
    {
        public Guid Id { get; set; }
    }

    public class GoodsReceiptDetailDto
    {
        public Guid Id { get; set; }
        public Guid? PurchaseOrderId { get; set; }
        public string? PurchaseOrderNumber { get; set; }
        public Guid SupplierId { get; set; }
        public string SupplierNameSnapshot { get; set; } = string.Empty;
        public DateOnly ReceiptDate { get; set; }
        public string WaybillNo { get; set; } = string.Empty;
        public DateOnly WaybillDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int StatusValue { get; set; }
        public string? Note { get; set; }
        public string? ExternalRef { get; set; }
        public bool IsEditable { get; set; }
        public List<GoodsReceiptLineDetailDto> Lines { get; set; } = new();
    }

    public class GoodsReceiptLineDetailDto
    {
        public Guid Id { get; set; }
        public Guid? PurchaseOrderLineId { get; set; }
        public int StockMasterId { get; set; }
        public string StockNameSnapshot { get; set; } = string.Empty;
        public string UnitSnapshot { get; set; } = string.Empty;
        public decimal ReceivedQty { get; set; }
        public decimal OrderedQty { get; set; }
        public decimal? AcceptedQty { get; set; }
        public decimal? RejectedQty { get; set; }
        public string? RejectReason { get; set; }
        public string? Note { get; set; }
    }

    public class GetGoodsReceiptDetailQueryHandler : IRequestHandler<GetGoodsReceiptDetailQuery, GoodsReceiptDetailDto>
    {
        private readonly IApplicationDbContext _context;

        public GetGoodsReceiptDetailQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GoodsReceiptDetailDto> Handle(GetGoodsReceiptDetailQuery request, CancellationToken cancellationToken)
        {
            var gr = await _context.GoodsReceipts
                .Include(p => p.Lines)
                .Include(p => p.PurchaseOrder)
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
            
            if (gr == null) throw new NotFoundException("GoodsReceipt", request.Id);

            var dto = new GoodsReceiptDetailDto
            {
                Id = gr.Id,
                PurchaseOrderId = gr.PurchaseOrderId,
                PurchaseOrderNumber = gr.PurchaseOrder?.OrderNumber,
                SupplierId = gr.SupplierId,
                SupplierNameSnapshot = gr.SupplierNameSnapshot,
                ReceiptDate = gr.ReceiptDate,
                WaybillNo = gr.WaybillNo,
                WaybillDate = gr.WaybillDate,
                Status = gr.Status.ToString(),
                StatusValue = (int)gr.Status,
                Note = gr.Note,
                ExternalRef = gr.ExternalRef,
                IsEditable = gr.IsEditable,
                Lines = gr.Lines.Select(l => new GoodsReceiptLineDetailDto
                {
                    Id = l.Id,
                    PurchaseOrderLineId = l.PurchaseOrderLineId,
                    StockMasterId = l.StockMasterId,
                    StockNameSnapshot = l.StockNameSnapshot ?? "",
                    UnitSnapshot = l.UnitSnapshot.ToString(),
                    ReceivedQty = l.ReceivedQty,
                    OrderedQty = l.OrderedQty,
                    AcceptedQty = l.AcceptedQty,
                    RejectedQty = l.RejectedQty,
                    RejectReason = l.RejectReason,
                    Note = l.Note
                }).ToList()
            };

            return dto;
        }
    }
}
