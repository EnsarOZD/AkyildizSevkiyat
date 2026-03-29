using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Projects.Commands.BulkUpdateDeliveryOrders
{
    public record ProjectOrderItem(int ProjectId, int DeliveryOrder);

    public record BulkUpdateDeliveryOrdersCommand(List<ProjectOrderItem> Orders) : IRequest, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager", "Dispatcher" };
    }

    public class BulkUpdateDeliveryOrdersCommandHandler : IRequestHandler<BulkUpdateDeliveryOrdersCommand>
    {
        private readonly IApplicationDbContext _context;

        public BulkUpdateDeliveryOrdersCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(BulkUpdateDeliveryOrdersCommand request, CancellationToken cancellationToken)
        {
            var ids = request.Orders.Select(o => o.ProjectId).ToList();
            var projects = await _context.Projects
                .Where(p => ids.Contains(p.Id))
                .ToListAsync(cancellationToken);

            var orderMap = request.Orders.ToDictionary(o => o.ProjectId, o => o.DeliveryOrder);
            foreach (var p in projects)
            {
                if (orderMap.TryGetValue(p.Id, out var order))
                    p.DeliveryOrder = order;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
