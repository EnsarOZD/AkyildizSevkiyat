using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Reports.Queries.GetPendingGoodsReceipts
{
    public record GetPendingGoodsReceiptsQuery : IRequest<List<PendingGoodsReceiptRow>>;

    public class PendingGoodsReceiptRow
    {
        public Guid Id { get; set; }
        public string WaybillNo { get; set; } = string.Empty;
        public DateOnly ReceiptDate { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string? LinkedOrderNumber { get; set; }
        public int LineCount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class GetPendingGoodsReceiptsQueryHandler : IRequestHandler<GetPendingGoodsReceiptsQuery, List<PendingGoodsReceiptRow>>
    {
        private readonly IApplicationDbContext _context;

        public GetPendingGoodsReceiptsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PendingGoodsReceiptRow>> Handle(GetPendingGoodsReceiptsQuery request, CancellationToken cancellationToken)
        {
            return await _context.GoodsReceipts
                .Include(gr => gr.Lines)
                .Include(gr => gr.PurchaseOrder)
                .Where(gr => gr.Status == GoodsReceiptStatus.Draft)
                .OrderBy(gr => gr.ReceiptDate)
                .Select(gr => new PendingGoodsReceiptRow
                {
                    Id                 = gr.Id,
                    WaybillNo          = gr.WaybillNo,
                    ReceiptDate        = gr.ReceiptDate,
                    SupplierName       = gr.SupplierNameSnapshot,
                    LinkedOrderNumber  = gr.PurchaseOrder != null ? gr.PurchaseOrder.OrderNumber : null,
                    LineCount          = gr.Lines.Count,
                    Status             = gr.Status.ToString()
                })
                .ToListAsync(cancellationToken);
        }
    }
}
