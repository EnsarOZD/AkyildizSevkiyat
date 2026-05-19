namespace Akyildiz.Sevkiyat.Application.GoodsReceipts.Commands.BatchUpdateLines
{
    public class BatchUpdateGoodsReceiptLinesCommand : IRequest<Unit>
    {
        public Guid GoodsReceiptId { get; set; }
        public List<LineUpdateItem> Lines { get; set; } = new();
    }

    public class LineUpdateItem
    {
        public Guid LineId { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal RejectedQty { get; set; }
        public string? RejectReason { get; set; }
    }

    public class BatchUpdateGoodsReceiptLinesCommandHandler : IRequestHandler<BatchUpdateGoodsReceiptLinesCommand, Unit>
    {
        private readonly IApplicationDbContext _context;

        public BatchUpdateGoodsReceiptLinesCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(BatchUpdateGoodsReceiptLinesCommand request, CancellationToken cancellationToken)
        {
            var gr = await _context.GoodsReceipts
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == request.GoodsReceiptId, cancellationToken);

            if (gr == null) throw new NotFoundException("GoodsReceipt", request.GoodsReceiptId);
            if (gr.Status != GoodsReceiptStatus.Draft)
                throw new DomainException("Taslak olmayan bir mal kabulünün kalemleri güncellenemez.");

            var lineDict = gr.Lines.ToDictionary(l => l.Id);

            foreach (var update in request.Lines)
            {
                if (!lineDict.TryGetValue(update.LineId, out var line))
                    throw new NotFoundException("GoodsReceiptLine", update.LineId);

                if (update.RejectedQty > update.ReceivedQty)
                    throw new DomainException($"Reddedilen miktar teslim alınan miktarı geçemez. (Kalem: {update.LineId})");

                if (update.RejectedQty > 0 && string.IsNullOrWhiteSpace(update.RejectReason))
                    throw new DomainException("Red nedeni, reddedilen miktar girildiğinde zorunludur.");

                line.ReceivedQty = update.ReceivedQty;
                line.RejectedQty = update.RejectedQty;
                line.AcceptedQty = update.ReceivedQty - update.RejectedQty;
                line.RejectReason = update.RejectedQty > 0 ? update.RejectReason : null;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
