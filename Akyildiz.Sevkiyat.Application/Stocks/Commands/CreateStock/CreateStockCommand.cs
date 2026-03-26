using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Stocks.Commands.CreateStock
{
    public record CreateStockCommand(string StockCode, string StockName, Akyildiz.Sevkiyat.Domain.Enums.StockUnit Unit, Akyildiz.Sevkiyat.Domain.Enums.StockCategory Category, Akyildiz.Sevkiyat.Domain.Enums.PickingType PickingType, decimal? UnitPrice, Akyildiz.Sevkiyat.Domain.Enums.TaxRate TaxRate) : IRequest<int>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting" };
    }

    public class CreateStockCommandHandler : IRequestHandler<CreateStockCommand, int>
    {
        private readonly IApplicationDbContext _context;

        public CreateStockCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(CreateStockCommand request, CancellationToken cancellationToken)
        {
            var normalizedCode = request.StockCode.Trim().ToUpperInvariant();

            var existing = await _context.StockMasters
                .FirstOrDefaultAsync(s => s.StockCode == normalizedCode, cancellationToken);
            
            if (existing != null)
            {
                // Already exists, maybe update name/zone? 
                // For now, just return existing id
                return existing.Id;
            }

            var entity = new StockMaster
            {
                StockCode = normalizedCode,
                StockName = request.StockName?.Trim() ?? string.Empty,
                Unit = request.Unit,
                Category = request.Category,
                PickingType = request.PickingType,
                UnitPrice = request.UnitPrice ?? 0,
                TaxRate = request.TaxRate,
                IsActive = true
            };

            _context.StockMasters.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            return entity.Id;
        }
    }
}
