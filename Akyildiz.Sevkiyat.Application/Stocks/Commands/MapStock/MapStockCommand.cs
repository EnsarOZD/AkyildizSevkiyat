using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Stocks.Commands.MapStock
{
    public record MapStockCommand(int MappingId, int? LocalStockId, bool Ignore) : IRequest;

    public class MapStockCommandHandler : IRequestHandler<MapStockCommand>
    {
        private readonly IApplicationDbContext _context;

        public MapStockCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(MapStockCommand request, CancellationToken cancellationToken)
        {
            var mapping = await _context.StockMappings.FindAsync(new object[] { request.MappingId }, cancellationToken);
            if (mapping == null) return;

            // 1. Update Mapping
            if (request.Ignore)
            {
                mapping.MatchStatus = MatchStatus.Ignored;
                mapping.LocalStockId = null;
            }
            else if (request.LocalStockId.HasValue)
            {
                mapping.MatchStatus = MatchStatus.Mapped;
                mapping.LocalStockId = request.LocalStockId;
            }
            else
            {
                mapping.MatchStatus = MatchStatus.Unmapped;
                mapping.LocalStockId = null;
            }

            // 2. Re-evaluate Affected Orders
            // Find orders containing this external stock code that are currently NeedsMapping
            // Note: This is an expensive query if we don't handle it carefully.
            // IssOrder -> Lines.
            
            // We need to find orders that contain THIS stock code.
            // And check if they are now Ready (meaning ALL lines are now mapped/ignored).
            // Wait, "Ignored" keeps it NeedsMapping. So only "Mapped" can fix it.
            
            if (mapping.MatchStatus == MatchStatus.Mapped)
            {
                var affectedOrders = await _context.IssOrders
                    .Include(o => o.Lines)
                    .Where(o => o.ImportStatus == ImportStatus.NeedsMapping && 
                                o.Lines.Any(l => l.StockCode == mapping.ExternalStockCode))
                    .ToListAsync(cancellationToken);

                foreach (var order in affectedOrders)
                {
                    // Check if ALL lines are now mapped
                     bool isReady = true;
                     var orderStockCodes = order.Lines.Select(l => l.StockCode).Distinct().ToList();
                     
                     // We need to check the status of ALL lines
                     // We can fetch all mappings for this order
                     var orderMappings = await _context.StockMappings
                         .Where(m => orderStockCodes.Contains(m.ExternalStockCode) && m.ExternalSystem == "ISS-IP")
                         .ToListAsync(cancellationToken);
                     
                     foreach (var lineCode in orderStockCodes)
                     {
                         var lineMapping = orderMappings.FirstOrDefault(m => m.ExternalStockCode == lineCode);
                         if (lineMapping == null || lineMapping.MatchStatus != MatchStatus.Mapped)
                         {
                             isReady = false;
                             break;
                         }
                     }

                     if (isReady)
                     {
                         order.ImportStatus = ImportStatus.Ready;
                     }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
