using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Orders.Commands.BulkDeactivateIssOrders
{
    public record BulkDeactivateIssOrdersCommand(IReadOnlyList<int> Ids) : IRequest<int>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting" };
    }

    public class BulkDeactivateIssOrdersCommandHandler : IRequestHandler<BulkDeactivateIssOrdersCommand, int>
    {
        private readonly IApplicationDbContext _context;

        public BulkDeactivateIssOrdersCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(BulkDeactivateIssOrdersCommand request, CancellationToken cancellationToken)
        {
            var orders = await _context.IssOrders
                .Where(o => request.Ids.Contains(o.Id) && o.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var order in orders)
                order.IsActive = false;

            await _context.SaveChangesAsync(cancellationToken);

            return orders.Count;
        }
    }
}
