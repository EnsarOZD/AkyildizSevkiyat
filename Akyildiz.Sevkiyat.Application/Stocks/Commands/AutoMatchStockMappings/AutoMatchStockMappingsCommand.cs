using Akyildiz.Sevkiyat.Application.Common;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Akyildiz.Sevkiyat.Application.Stocks.Commands.AutoMatchStockMappings
{
    public record AutoMatchStockMappingsCommand : IRequest<AutoMatchStockMappingsResult>;

    public class AutoMatchStockMappingsResult
    {
        public int MatchedCount { get; set; }
        public int UnmatchedCount { get; set; }
        public int OrdersUnlocked { get; set; } // NeedsMapping → Ready
    }

    public class AutoMatchStockMappingsCommandHandler : IRequestHandler<AutoMatchStockMappingsCommand, AutoMatchStockMappingsResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<AutoMatchStockMappingsCommandHandler> _logger;

        public AutoMatchStockMappingsCommandHandler(IApplicationDbContext context, ILogger<AutoMatchStockMappingsCommandHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AutoMatchStockMappingsResult> Handle(AutoMatchStockMappingsCommand request, CancellationToken cancellationToken)
        {
            // Build case-insensitive name → id lookup (only names that are unique in StockMaster)
            var allStocks = await _context.StockMasters
                .Where(s => s.IsActive)
                .Select(s => new { s.Id, s.StockName })
                .ToListAsync(cancellationToken);

            var uniqueByName = allStocks
                .GroupBy(s => TextNormalizer.NormalizeForComparison(s.StockName))
                .Where(g => g.Count() == 1)
                .ToDictionary(g => g.Key, g => g.First().Id);

            _logger.LogInformation("AutoMatch: Found {TotalStocks} active stocks. Generated {UniqueKeys} unique normalization keys.", 
                allStocks.Count, uniqueByName.Count);

            // Get all unmapped entries
            var unmapped = await _context.StockMappings
                .Where(m => m.MatchStatus == MatchStatus.Unmapped)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("AutoMatch: Processing {UnmappedCount} unmapped entries.", unmapped.Count);

            var result = new AutoMatchStockMappingsResult { UnmatchedCount = unmapped.Count };

            foreach (var mapping in unmapped)
            {
                var nameKey = TextNormalizer.NormalizeForComparison(mapping.ExternalStockName);
                if (!string.IsNullOrEmpty(nameKey) && uniqueByName.TryGetValue(nameKey, out var stockId))
                {
                    mapping.LocalStockId = stockId;
                    mapping.MatchStatus = MatchStatus.Mapped;
                    result.MatchedCount++;
                }
            }

            if (result.MatchedCount == 0)
                return result;

            await _context.SaveChangesAsync(cancellationToken);
            result.UnmatchedCount -= result.MatchedCount;

            _logger.LogInformation("AutoMatch: {Matched} mappings resolved by name.", result.MatchedCount);

            // Re-evaluate NeedsMapping orders
            var pendingOrderIds = await _context.IssOrders
                .Where(o => o.ImportStatus == ImportStatus.NeedsMapping && o.IsActive)
                .Select(o => o.Id)
                .ToListAsync(cancellationToken);

            foreach (var orderId in pendingOrderIds)
            {
                var stockCodes = await _context.IssOrderLines
                    .Where(l => l.IssOrderId == orderId)
                    .Select(l => l.StockCode)
                    .ToListAsync(cancellationToken);

                bool allMapped = true;
                foreach (var code in stockCodes)
                {
                    var isMapped = await _context.StockMappings
                        .AnyAsync(m => m.ExternalStockCode == code &&
                                       (m.MatchStatus == MatchStatus.Mapped || m.MatchStatus == MatchStatus.Ignored),
                                  cancellationToken);
                    if (!isMapped) { allMapped = false; break; }
                }

                if (allMapped)
                {
                    await _context.IssOrders
                        .Where(o => o.Id == orderId)
                        .ExecuteUpdateAsync(s => s.SetProperty(o => o.ImportStatus, ImportStatus.Ready), cancellationToken);
                    result.OrdersUnlocked++;
                }
            }

            return result;
        }
    }
}
