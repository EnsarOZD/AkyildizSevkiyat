using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Orders.Commands.ToggleActive
{
    public record ToggleIssOrderActiveCommand(int Id, bool IsActive) : IRequest, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting" };
    }

    public class ToggleIssOrderActiveCommandHandler : IRequestHandler<ToggleIssOrderActiveCommand>
    {
        private readonly IApplicationDbContext _context;

        public ToggleIssOrderActiveCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(ToggleIssOrderActiveCommand request, CancellationToken cancellationToken)
        {
            var order = await _context.IssOrders.FindAsync(new object[] { request.Id }, cancellationToken);

            if (order == null)
            {
                // Handle not found if necessary, or just return
                return;
            }

            order.IsActive = request.IsActive;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
