using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Customers.Commands.ToggleManualCustomerActive
{
    public record ToggleManualCustomerActiveCommand(int Id, bool IsActive)
        : IRequest, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager", "Accounting" };
    }

    public class ToggleManualCustomerActiveCommandHandler
        : IRequestHandler<ToggleManualCustomerActiveCommand>
    {
        private readonly IApplicationDbContext _context;

        public ToggleManualCustomerActiveCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(ToggleManualCustomerActiveCommand request, CancellationToken cancellationToken)
        {
            var customer = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == request.Id && p.Source == ProjectSource.Manual, cancellationToken);

            if (customer is null)
                throw new NotFoundException($"Manuel müşteri #{request.Id} bulunamadı.");

            customer.IsActive = request.IsActive;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
