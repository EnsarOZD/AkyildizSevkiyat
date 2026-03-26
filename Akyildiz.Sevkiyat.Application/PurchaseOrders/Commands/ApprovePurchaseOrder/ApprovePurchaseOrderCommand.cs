using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Application.PurchaseOrders.Commands.ApprovePurchaseOrder
{
    public class ApprovePurchaseOrderCommand : IRequest<Unit>, IRequireRoles
    {
        public Guid Id { get; set; }

        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager" };
    }

    public class ApprovePurchaseOrderCommandHandler : IRequestHandler<ApprovePurchaseOrderCommand, Unit>
    {
        private readonly IApplicationDbContext _context;

        public ApprovePurchaseOrderCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(ApprovePurchaseOrderCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.PurchaseOrders
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
            
            if (entity == null) throw new NotFoundException("PurchaseOrder", request.Id);

            if (entity.Status != PurchaseOrderStatus.Draft)
            {
                throw new DomainException($"Cannot approve Purchase Order in status {entity.Status}. Only Draft can be approved.");
            }

            entity.Status = PurchaseOrderStatus.Approved;
            
            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
