using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Suppliers.Commands.DeleteSupplier
{
    public record DeleteSupplierCommand(Guid Id) : IRequest;

    public class DeleteSupplierCommandHandler : IRequestHandler<DeleteSupplierCommand>
    {
        private readonly IApplicationDbContext _context;

        public DeleteSupplierCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(DeleteSupplierCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.Suppliers.FindAsync(new object[] { request.Id }, cancellationToken);
            if (entity == null) throw new NotFoundException("Supplier", request.Id);

            var hasPurchaseOrders = await _context.PurchaseOrders
                .AnyAsync(po => po.SupplierId == request.Id, cancellationToken);

            if (hasPurchaseOrders)
                throw new DomainException("Bu tedarikçiye ait satınalma siparişleri mevcut olduğu için silinemez.");

            _context.Suppliers.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
