using Akyildiz.Sevkiyat.Application.Common.Interfaces;

namespace Akyildiz.Sevkiyat.Application.PurchaseOrders.Commands.UpdatePurchaseOrderLine
{
    public class UpdatePurchaseOrderLineCommand : IRequest<Unit>
    {
        public Guid PurchaseOrderId { get; set; }
        public Guid LineId { get; set; }
        public decimal OrderedQty { get; set; }
        public decimal? UnitPrice { get; set; }
        public string? Note { get; set; }
    }

    public class UpdatePurchaseOrderLineCommandHandler : IRequestHandler<UpdatePurchaseOrderLineCommand, Unit>
    {
        private readonly IApplicationDbContext _context;

        public UpdatePurchaseOrderLineCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(UpdatePurchaseOrderLineCommand request, CancellationToken cancellationToken)
        {
            var po = await _context.PurchaseOrders
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == request.PurchaseOrderId, cancellationToken);

            if (po == null) throw new NotFoundException("PurchaseOrder", request.PurchaseOrderId);

            if (!po.IsEditable)
                throw new DomainException($"Purchase Order '{po.OrderNumber}' is not editable. Current status: {po.Status}.");

            var line = po.Lines.FirstOrDefault(x => x.Id == request.LineId);
            if (line == null) throw new NotFoundException("PurchaseOrderLine", request.LineId);

            line.OrderedQty = request.OrderedQty;
            line.UnitPrice = request.UnitPrice;
            line.Note = request.Note;

            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
