using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.SaveOutOfCityPickingProgress
{
    public record OutOfCityProgressLineDto(int ShipmentLineId, decimal DeliveredQty, int? NewLocalStockId = null);

    public record SaveOutOfCityPickingProgressCommand : IRequest<Unit>, IRequireRoles
    {
        public int ZonePreparationId { get; init; }
        public List<OutOfCityProgressLineDto> Lines { get; init; } = new();

        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting", "Warehouse" };
    }

    public class SaveOutOfCityPickingProgressCommandHandler : IRequestHandler<SaveOutOfCityPickingProgressCommand, Unit>
    {
        private readonly IApplicationDbContext _context;

        public SaveOutOfCityPickingProgressCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(SaveOutOfCityPickingProgressCommand request, CancellationToken cancellationToken)
        {
            var zp = await _context.ZonePreparations
                .Include(z => z.Zone)
                .FirstOrDefaultAsync(z => z.Id == request.ZonePreparationId, cancellationToken)
                ?? throw new NotFoundException("ZonePreparation", request.ZonePreparationId);

            if (zp.Zone?.IsOutOfCity != true)
                throw new DomainException("Bu komut yalnızca şehir dışı bölgeler için kullanılabilir.");

            if (request.Lines.Count == 0)
                return Unit.Value;

            var lineIds = request.Lines.Select(l => l.ShipmentLineId).ToList();
            var dbLines = await _context.ShipmentLines
                .Where(l => lineIds.Contains(l.Id))
                .ToListAsync(cancellationToken);

            var substituteStockIds = request.Lines
                .Where(l => l.NewLocalStockId.HasValue)
                .Select(l => l.NewLocalStockId!.Value)
                .Distinct()
                .ToList();

            var substituteStocks = substituteStockIds.Count > 0
                ? await _context.StockMasters
                    .Where(s => substituteStockIds.Contains(s.Id))
                    .ToListAsync(cancellationToken)
                : new List<Domain.Entities.StockMaster>();

            var lineMap = request.Lines.ToDictionary(l => l.ShipmentLineId);

            foreach (var line in dbLines)
            {
                if (!lineMap.TryGetValue(line.Id, out var update)) continue;

                if (update.NewLocalStockId.HasValue)
                {
                    var stock = substituteStocks.FirstOrDefault(s => s.Id == update.NewLocalStockId.Value);
                    if (stock != null)
                        line.UpdateStockInfo(stock.StockCode, stock.StockName, stock.Unit, stock.Id, true);
                }

                line.SavePickingProgress(update.DeliveredQty);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
