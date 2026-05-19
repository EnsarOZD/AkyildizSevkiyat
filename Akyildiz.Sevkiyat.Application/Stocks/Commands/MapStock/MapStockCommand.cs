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
            // Hem Mapped hem Ignored durumları siparişi Ready'e taşıyabilir.
            // Tüm satırları Mapped veya Ignored olan NeedsMapping siparişler Ready'e geçer.
            if (mapping.MatchStatus == MatchStatus.Mapped || mapping.MatchStatus == MatchStatus.Ignored)
            {
                var affectedOrders = await _context.IssOrders
                    .Include(o => o.Lines)
                    .Where(o => o.ImportStatus == ImportStatus.NeedsMapping &&
                                o.Lines.Any(l => l.StockCode == mapping.ExternalStockCode))
                    .ToListAsync(cancellationToken);

                foreach (var order in affectedOrders)
                {
                    var orderStockCodes = order.Lines.Select(l => l.StockCode).Distinct().ToList();
                    var orderMappings = await _context.StockMappings
                        .Where(m => orderStockCodes.Contains(m.ExternalStockCode) && m.ExternalSystem == "ISS-IP")
                        .ToListAsync(cancellationToken);

                    bool isReady = orderStockCodes.All(code =>
                    {
                        var m = orderMappings.FirstOrDefault(x => x.ExternalStockCode == code);
                        return m != null && (m.MatchStatus == MatchStatus.Mapped || m.MatchStatus == MatchStatus.Ignored);
                    });

                    if (isReady)
                        order.ImportStatus = ImportStatus.Ready;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
