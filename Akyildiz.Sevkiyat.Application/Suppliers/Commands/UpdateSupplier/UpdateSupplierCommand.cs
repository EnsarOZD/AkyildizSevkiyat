using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.Suppliers.Commands.UpdateSupplier
{
    public class UpdateSupplierCommand : IRequest<Unit>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? SupplierCode { get; set; }
        public string? Email { get; set; }
    }

    public class UpdateSupplierCommandHandler : IRequestHandler<UpdateSupplierCommand, Unit>
    {
        private readonly IApplicationDbContext _context;

        public UpdateSupplierCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(UpdateSupplierCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new DomainException("Tedarikçi adı zorunludur.");

            var entity = await _context.Suppliers.FindAsync(new object[] { request.Id }, cancellationToken);
            if (entity == null) throw new NotFoundException("Supplier", request.Id);

            entity.Name = request.Name;
            entity.SupplierCode = request.SupplierCode;
            entity.Email = request.Email;

            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
