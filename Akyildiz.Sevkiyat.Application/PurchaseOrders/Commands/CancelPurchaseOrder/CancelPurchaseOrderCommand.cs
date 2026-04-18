using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Application.PurchaseOrders.Commands.CancelPurchaseOrder
{
    public class CancelPurchaseOrderCommand : IRequest<Unit>, IRequireRoles
    {
        public Guid Id { get; set; }

        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting" };
    }

    public class CancelPurchaseOrderCommandHandler : IRequestHandler<CancelPurchaseOrderCommand, Unit>
    {
        private readonly IApplicationDbContext _context;

        public CancelPurchaseOrderCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(CancelPurchaseOrderCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.PurchaseOrders
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
            
            if (entity == null) throw new NotFoundException("PurchaseOrder", request.Id);

            if (entity.Status == PurchaseOrderStatus.Cancelled)
            {
                 // Already cancelled, idempotent success or throw? Throw for UI feedback.
                 throw new DomainException("Purchase Order is already cancelled.");
            }

            // Validation: Cannot cancel if any POSTED GoodsReceipt exists linked to this PO
            var hasPostedReceipts = await _context.GoodsReceipts
                .AnyAsync(x => x.PurchaseOrderId == request.Id && x.Status == GoodsReceiptStatus.Posted, cancellationToken);

            if (hasPostedReceipts)
            {
                throw new DomainException("Cannot cancel Purchase Order because there are posted Goods Receipts linked to it.");
            }

            entity.Status = PurchaseOrderStatus.Cancelled;
            
            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
