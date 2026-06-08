using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.ClothingPreparation
{
    public record AggPickInput(string StockCode, decimal PickedQty, string? DifferenceReason);

    /// <summary>
    /// Konsolide toplamada girilen ürün-bazlı toplam miktarı, seçilen sevkiyatların
    /// satırlarına FIFO (sevkiyat sırasına göre) dağıtır. Eksik kalan satırlara sebep yazılır.
    /// </summary>
    public record UpdateClothingAggregatedLinesCommand(List<int> ShipmentIds, List<AggPickInput> Lines)
        : IRequest<Unit>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting", "Warehouse" };
    }

    public class UpdateClothingAggregatedLinesCommandHandler : IRequestHandler<UpdateClothingAggregatedLinesCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        public UpdateClothingAggregatedLinesCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Unit> Handle(UpdateClothingAggregatedLinesCommand request, CancellationToken cancellationToken)
        {
            var shipments = await _context.Shipments
                .Where(s => request.ShipmentIds.Contains(s.Id) && s.OperationType == OperationType.Clothing)
                .Include(s => s.Lines)
                .OrderBy(s => s.Id)
                .ToListAsync(cancellationToken);

            // Stok kodu → o stoğun tüm satırları (sevkiyat sırasına göre, FIFO)
            var linesByStock = shipments
                .SelectMany(s => s.Lines)
                .GroupBy(l => l.StockCode)
                .ToDictionary(g => g.Key, g => g.OrderBy(l => l.ShipmentId).ToList());

            foreach (var input in request.Lines)
            {
                if (!linesByStock.TryGetValue(input.StockCode, out var lines)) continue;

                var remaining = input.PickedQty;
                var reason = string.IsNullOrWhiteSpace(input.DifferenceReason) ? null : input.DifferenceReason.Trim();

                foreach (var line in lines)
                {
                    var give = Math.Max(0, Math.Min(remaining, line.OrderedQty));
                    remaining -= give;

                    // Eksik (give < ordered) ise sebep gerekir; SetDeliveredQty bunu zorunlu kılar.
                    var lineReason = give != line.OrderedQty ? reason : null;
                    line.SetDeliveredQty(give, lineReason, "Konsolide toplama");
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
