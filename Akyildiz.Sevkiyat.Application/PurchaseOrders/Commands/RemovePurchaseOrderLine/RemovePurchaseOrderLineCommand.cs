using Akyildiz.Sevkiyat.Application.Common.Interfaces;

namespace Akyildiz.Sevkiyat.Application.PurchaseOrders.Commands.RemovePurchaseOrderLine
{
    public class RemovePurchaseOrderLineCommand : IRequest<Unit>
    {
        public Guid PurchaseOrderId { get; set; }
        public Guid LineId { get; set; }
    }

    public class RemovePurchaseOrderLineCommandHandler : IRequestHandler<RemovePurchaseOrderLineCommand, Unit>
    {
        private readonly IApplicationDbContext _context;

        public RemovePurchaseOrderLineCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(RemovePurchaseOrderLineCommand request, CancellationToken cancellationToken)
        {
            var po = await _context.PurchaseOrders
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == request.PurchaseOrderId, cancellationToken);

            if (po == null) throw new NotFoundException("PurchaseOrder", request.PurchaseOrderId);

            if (!po.IsEditable)
                throw new DomainException($"Purchase Order '{po.OrderNumber}' is not editable. Current status: {po.Status}.");

            var line = po.Lines.FirstOrDefault(x => x.Id == request.LineId);
            if (line == null) throw new NotFoundException("PurchaseOrderLine", request.LineId);

            if (po.Lines.Count <= 1)
                throw new DomainException("En az bir satır bulunması zorunludur. Son satır silinemez.");

            _context.PurchaseOrderLines.Remove(line);
            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
