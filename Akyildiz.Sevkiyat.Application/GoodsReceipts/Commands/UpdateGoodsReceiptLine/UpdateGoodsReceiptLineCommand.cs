using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Application.GoodsReceipts.Commands.UpdateGoodsReceiptLine
{
    public class UpdateGoodsReceiptLineCommand : IRequest<Unit>
    {
        public Guid GoodsReceiptId { get; set; }
        public Guid LineId { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal? RejectedQty { get; set; }
        public string? RejectReason { get; set; }
        public string? Note { get; set; }
    }

    public class UpdateGoodsReceiptLineCommandHandler : IRequestHandler<UpdateGoodsReceiptLineCommand, Unit>
    {
        private readonly IApplicationDbContext _context;

        public UpdateGoodsReceiptLineCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(UpdateGoodsReceiptLineCommand request, CancellationToken cancellationToken)
        {
            var gr = await _context.GoodsReceipts
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == request.GoodsReceiptId, cancellationToken);
            
            if (gr == null) throw new NotFoundException("GoodsReceipt", request.GoodsReceiptId);
            
            if (gr.Status != GoodsReceiptStatus.Draft) 
            {
                throw new DomainException("Cannot update lines of a non-Draft Goods Receipt.");
            }

            var line = gr.Lines.FirstOrDefault(x => x.Id == request.LineId);
            if (line == null) throw new NotFoundException("GoodsReceiptLine", request.LineId);

            if (request.RejectedQty.HasValue && request.RejectedQty.Value > request.ReceivedQty)
            {
                throw new DomainException("Rejected quantity cannot exceed received quantity.");
            }

            if (request.RejectedQty.HasValue && request.RejectedQty.Value > 0)
            {
                if (string.IsNullOrWhiteSpace(request.RejectReason))
                {
                    throw new DomainException("Reject reason is required when there is a rejection.");
                }
            }
            else if (request.RejectedQty.HasValue && request.RejectedQty.Value == 0)
            {
                // If quantity is 0, clear the reason
                request.RejectReason = null;
            }

            line.ReceivedQty = request.ReceivedQty;
            line.RejectedQty = request.RejectedQty ?? 0;
            line.AcceptedQty = request.ReceivedQty - (request.RejectedQty ?? 0);
            line.RejectReason = request.RejectReason;
            line.Note = request.Note;

            await _context.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
