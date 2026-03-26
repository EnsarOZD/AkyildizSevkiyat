using Akyildiz.Sevkiyat.Application.Common.Interfaces;

namespace Akyildiz.Sevkiyat.Application.GoodsReceipts.Commands.RemoveGoodsReceiptLine
{
    public class RemoveGoodsReceiptLineCommand : IRequest<Unit>
    {
        public Guid GoodsReceiptId { get; set; }
        public Guid LineId { get; set; }
    }

    public class RemoveGoodsReceiptLineCommandHandler : IRequestHandler<RemoveGoodsReceiptLineCommand, Unit>
    {
        private readonly IApplicationDbContext _context;

        public RemoveGoodsReceiptLineCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(RemoveGoodsReceiptLineCommand request, CancellationToken cancellationToken)
        {
            var gr = await _context.GoodsReceipts
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == request.GoodsReceiptId, cancellationToken);

            if (gr == null) throw new NotFoundException("GoodsReceipt", request.GoodsReceiptId);

            if (!gr.IsEditable)
                throw new DomainException($"Mal girişi belgesi düzenlenemez. Mevcut durum: {gr.Status}.");

            var line = gr.Lines.FirstOrDefault(x => x.Id == request.LineId);
            if (line == null) throw new NotFoundException("GoodsReceiptLine", request.LineId);

            if (gr.Lines.Count <= 1)
                throw new DomainException("En az bir satır bulunması zorunludur. Son satır silinemez.");

            _context.GoodsReceiptLines.Remove(line);
            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
