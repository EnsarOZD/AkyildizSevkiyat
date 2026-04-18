using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Application.PurchaseOrders.Commands.ClosePurchaseOrder
{
    public class ClosePurchaseOrderCommand : IRequest<Unit>, IRequireRoles
    {
        public Guid Id { get; set; }

        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting" };
    }

    public class ClosePurchaseOrderCommandHandler : IRequestHandler<ClosePurchaseOrderCommand, Unit>
    {
        private readonly IApplicationDbContext _context;

        public ClosePurchaseOrderCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(ClosePurchaseOrderCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.PurchaseOrders
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
            
            if (entity == null) throw new NotFoundException("PurchaseOrder", request.Id);

            // Allow Approved or PartiallyReceived to be closed.
            if (entity.Status != PurchaseOrderStatus.Approved && entity.Status != PurchaseOrderStatus.PartiallyReceived)
            {
                throw new DomainException($"Cannot close Purchase Order in status {entity.Status}. Only Approved or PartiallyReceived orders can be closed.");
            }

            entity.Status = PurchaseOrderStatus.Closed;
            
            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
