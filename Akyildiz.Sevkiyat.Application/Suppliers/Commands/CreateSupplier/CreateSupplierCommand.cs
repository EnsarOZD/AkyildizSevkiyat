using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Suppliers.Commands.CreateSupplier
{
    public class CreateSupplierCommand : IRequest<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public string? SupplierCode { get; set; }
        public string? Email { get; set; }
    }

    public class CreateSupplierCommandHandler : IRequestHandler<CreateSupplierCommand, Guid>
    {
        private readonly IApplicationDbContext _context;

        public CreateSupplierCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
        {
            // Validation simple for now
            if (string.IsNullOrWhiteSpace(request.Name)) throw new DomainException("Supplier Name required");

            var entity = new Supplier
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                SupplierCode = request.SupplierCode,
                Email = request.Email,
                IsActive = true
            };

            _context.Suppliers.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return entity.Id;
        }
    }
}
