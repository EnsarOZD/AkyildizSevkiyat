using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Orders.Commands.CheckOrderMappingStatus
{
    public record CheckOrderMappingStatusCommand : IRequest<int>;

    public class CheckOrderMappingStatusCommandHandler : IRequestHandler<CheckOrderMappingStatusCommand, int>
    {
        private readonly IApplicationDbContext _context;

        public CheckOrderMappingStatusCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(CheckOrderMappingStatusCommand request, CancellationToken cancellationToken)
        {
            var pendingOrders = await _context.IssOrders
                .Include(o => o.Lines)
                .Where(o => o.ImportStatus == ImportStatus.NeedsMapping && o.IsActive)
                .ToListAsync(cancellationToken);

            if (!pendingOrders.Any()) return 0;

            var mappings = await _context.StockMappings
                .Where(m => m.MatchStatus == MatchStatus.Mapped || m.MatchStatus == MatchStatus.Ignored)
                .Select(m => m.ExternalStockCode)
                .ToListAsync(cancellationToken);
            
            var validStockCodes = new HashSet<string>(mappings);

            int updatedCount = 0;

            foreach (var order in pendingOrders)
            {
                bool allMapped = true;
                foreach (var line in order.Lines)
                {
                    if (!validStockCodes.Contains(line.StockCode))
                    {
                        allMapped = false;
                        break;
                    }
                }

                if (allMapped)
                {
                    order.ImportStatus = ImportStatus.Ready;
                    updatedCount++;
                }
            }

            if (updatedCount > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            return updatedCount;
        }
    }
}
