using Akyildiz.Sevkiyat.Application.Common.Interfaces;

namespace Akyildiz.Sevkiyat.Application.PurchaseOrders.Commands.UpdatePurchaseOrder
{
    public class UpdatePurchaseOrderCommand : IRequest<Unit>
    {
        public Guid Id { get; set; }
        public DateOnly OrderDate { get; set; }
        public DateOnly? ExpectedDeliveryDate { get; set; }
        public string? Note { get; set; }
    }

    public class UpdatePurchaseOrderCommandHandler : IRequestHandler<UpdatePurchaseOrderCommand, Unit>
    {
        private readonly IApplicationDbContext _context;

        public UpdatePurchaseOrderCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(UpdatePurchaseOrderCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.PurchaseOrders
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (entity == null) throw new NotFoundException("PurchaseOrder", request.Id);

            if (!entity.IsEditable)
                throw new DomainException($"Purchase Order '{entity.OrderNumber}' is not editable. Current status: {entity.Status}.");

            entity.OrderDate = request.OrderDate;
            entity.ExpectedDeliveryDate = request.ExpectedDeliveryDate;
            entity.Note = request.Note;

            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
