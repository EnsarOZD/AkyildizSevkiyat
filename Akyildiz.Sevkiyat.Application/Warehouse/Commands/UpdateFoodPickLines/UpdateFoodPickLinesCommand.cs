using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.UpdateFoodPickLines
{
    public class FoodPickLineUpdateDto
    {
        /// <summary>Aynı stok koduna ait tüm satır ID'leri (farklı batch/proje).</summary>
        public List<int> ShipmentLineIds { get; set; } = new();
        /// <summary>Toplam toplanan miktar — satırlara FIFO ile dağıtılır.</summary>
        public decimal NewTotalPickedQty { get; set; }
        public string? DifferenceReason { get; set; }
        /// <summary>Yerine başka bir stok veriliyorsa yerel StockMaster ID'si.</summary>
        public int? NewLocalStockId { get; set; }
    }

    public record UpdateFoodPickLinesCommand : IRequest<bool>, IRequireRoles
    {
        public List<FoodPickLineUpdateDto> Updates { get; init; } = new();

        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting", "Warehouse", "Driver" };
    }

    public class UpdateFoodPickLinesCommandHandler : IRequestHandler<UpdateFoodPickLinesCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public UpdateFoodPickLinesCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<bool> Handle(UpdateFoodPickLinesCommand request, CancellationToken cancellationToken)
        {
            if (!request.Updates.Any()) return false;

            var allLineIds = request.Updates.SelectMany(u => u.ShipmentLineIds).ToList();

            var allLines = await _context.ShipmentLines
                .Include(sl => sl.Shipment)
                .Where(sl => allLineIds.Contains(sl.Id))
                .OrderBy(sl => sl.Id)
                .ToListAsync(cancellationToken);

            // Preload substitute stocks
            var substituteIds = request.Updates
                .Where(u => u.NewLocalStockId.HasValue)
                .Select(u => u.NewLocalStockId!.Value)
                .Distinct().ToList();

            var substituteStocks = substituteIds.Count > 0
                ? await _context.StockMasters
                    .Where(s => substituteIds.Contains(s.Id))
                    .ToListAsync(cancellationToken)
                : new List<Domain.Entities.StockMaster>();

            foreach (var update in request.Updates)
            {
                if (!update.ShipmentLineIds.Any()) continue;

                var lines = allLines
                    .Where(l => update.ShipmentLineIds.Contains(l.Id))
                    .OrderBy(l => l.Id)
                    .ToList();

                if (!lines.Any()) continue;

                // Stok değiştirme — tüm satırlara uygula
                if (update.NewLocalStockId.HasValue)
                {
                    var sub = substituteStocks.FirstOrDefault(s => s.Id == update.NewLocalStockId.Value);
                    if (sub != null)
                    {
                        foreach (var line in lines)
                            line.UpdateStockInfo(sub.StockCode, sub.StockName, sub.Unit, sub.Id, true);
                    }
                }

                decimal qtyToDistribute = update.NewTotalPickedQty;
                decimal totalOrdered = lines.Sum(l => l.OrderedQty);

                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    decimal qtyForLine;

                    if (i == lines.Count - 1)
                    {
                        qtyForLine = qtyToDistribute;
                    }
                    else if (qtyToDistribute >= line.OrderedQty)
                    {
                        qtyForLine = line.OrderedQty;
                        qtyToDistribute -= line.OrderedQty;
                    }
                    else
                    {
                        qtyForLine = qtyToDistribute;
                        qtyToDistribute = 0;
                    }

                    if (line.DeliveredQty != qtyForLine)
                    {
                        var reason = qtyForLine != line.OrderedQty
                            ? (update.DifferenceReason ?? "Gıda Toplu Toplama")
                            : null;

                        line.SetDeliveredQty(qtyForLine, reason, "Gıda Hazırlık Otomatik Dağıtım");

                        _context.ShipmentHistories.Add(new Domain.Entities.ShipmentHistory
                        {
                            ShipmentId      = line.ShipmentId,
                            OldStatus       = line.Shipment.Status,
                            NewStatus       = line.Shipment.Status,
                            ChangedAt       = System.DateTime.UtcNow,
                            ChangedByUserId = _currentUserService.UserId,
                            Description     = $"Gıda Dağıtım: {qtyForLine} adet " +
                                              $"(Toplam girilen: {update.NewTotalPickedQty}, Sipariş: {totalOrdered})"
                        });
                    }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
