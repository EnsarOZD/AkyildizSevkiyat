using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.MarkZoneMacroReady
{
    public record MarkZoneMacroReadyCommand : IRequest<MarkZoneMacroReadyResult>, IRequireRoles
    {
        public int ZonePreparationId { get; init; }
        /// <summary>
        /// Eksik macro satırlar olsa bile devam et (kasıtlı eksik toplama).
        /// ForceReason zorunludur.
        /// </summary>
        public bool ForceComplete { get; init; } = false;
        public string? ForceReason { get; init; }

        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting", "Warehouse", "Driver" };
    }

    public class MarkZoneMacroReadyResult
    {
        public bool Success { get; set; }
        /// <summary>DeliveredQty = 0 olan macro satır sayısı (gıda hariç).</summary>
        public int UnfilledMacroLineCount { get; set; }
        /// <summary>Gıda macro kalemi bulunduğu için GidaHazirlik aşamasına geçildi.</summary>
        public bool AdvancedToFoodPrep { get; set; }
    }

    public class MarkZoneMacroReadyCommandHandler : IRequestHandler<MarkZoneMacroReadyCommand, MarkZoneMacroReadyResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public MarkZoneMacroReadyCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<MarkZoneMacroReadyResult> Handle(MarkZoneMacroReadyCommand request, CancellationToken cancellationToken)
        {
            var zp = await _context.ZonePreparations
                .Include(z => z.Projects)
                .FirstOrDefaultAsync(z => z.Id == request.ZonePreparationId, cancellationToken);

            if (zp == null) throw new NotFoundException("ZonePreparation", request.ZonePreparationId);

            if (zp.Status < ZonePreparationStatus.MicroReady)
                throw new DomainException("Macro toplama aşamasına geçebilmek için Micro toplamanın tamamlanmış olması gerekir.");

            if (zp.Status >= ZonePreparationStatus.GidaHazirlik)
                throw new DomainException("Bu hazırlık zaten gıda hazırlık aşamasında veya ilerleyen bir aşamada.");

            if (zp.Projects.Any(p => !p.IsMicroReady))
                throw new DomainException("Hâlâ tamamlanmamış Micro toplama yapılan projeler var.");

            // Macro satırları sorgula — kategori bilgisiyle birlikte
            var macroLinesData = await _context.ShipmentLines
                .Where(l => l.Shipment.ZonePreparationId == zp.Id
                         && l.Shipment.Status != ShipmentStatus.Cancelled
                         && l.Shipment.Status != ShipmentStatus.Passive
                         && l.OrderedQty > 0)
                .Select(l => new {
                    l.Id,
                    l.DeliveredQty,
                    l.StockMasterId,
                    StockCode = l.IssOrderLine != null ? l.IssOrderLine.StockCode : l.StockCode,
                    PickingType = l.StockMaster != null ? (PickingType?)l.StockMaster.PickingType : null,
                    Category    = l.StockMaster != null ? (StockCategory?)l.StockMaster.Category : null
                })
                .ToListAsync(cancellationToken);

            var externalCodes = macroLinesData.Select(l => l.StockCode).Distinct().ToList();
            var mappings = await _context.StockMappings
                .Include(m => m.LocalStock)
                .Where(m => externalCodes.Contains(m.ExternalStockCode))
                .ToListAsync(cancellationToken);

            // Macro satırları kategoriyle işle
            var processedMacroLines = new List<(int Id, decimal DeliveredQty, StockCategory Category)>();

            foreach (var line in macroLinesData)
            {
                bool isMacro = false;
                StockCategory category = StockCategory.Tanimsiz;

                if (line.StockMasterId.HasValue && line.PickingType.HasValue)
                {
                    isMacro = line.PickingType == PickingType.Macro;
                    category = line.Category ?? StockCategory.Tanimsiz;
                }
                else
                {
                    var mapping = mappings.FirstOrDefault(m => m.ExternalStockCode.Equals(line.StockCode, StringComparison.OrdinalIgnoreCase));
                    if (mapping?.LocalStock != null)
                    {
                        isMacro = mapping.LocalStock.PickingType == PickingType.Macro;
                        category = mapping.LocalStock.Category;
                    }
                }

                if (isMacro)
                    processedMacroLines.Add((line.Id, line.DeliveredQty, category));
            }

            // Gıda dışı macro satırların eksiklik kontrolü
            var nonFoodMacroLines = processedMacroLines.Where(l => l.Category != StockCategory.Gida).ToList();
            var hasFoodMacroItems = processedMacroLines.Any(l => l.Category == StockCategory.Gida);

            var unfilledMacroCount = nonFoodMacroLines.Count(l => l.DeliveredQty == 0);

            if (unfilledMacroCount > 0 && !request.ForceComplete)
                throw new DomainException(
                    $"{unfilledMacroCount} makro satır henüz toplanmadı. " +
                    "Eksik satırlarla devam etmek için ForceComplete=true ve ForceReason gönderin.");

            if (request.ForceComplete && string.IsNullOrWhiteSpace(request.ForceReason))
                throw new DomainException("ForceComplete=true kullanılırken ForceReason zorunludur.");

            // Gıda macro kalemi varsa GidaHazirlik'e, yoksa doğrudan ReadyForDriverInfo'ya geç
            zp.Status = hasFoodMacroItems
                ? ZonePreparationStatus.GidaHazirlik
                : ZonePreparationStatus.ReadyForDriverInfo;

            zp.ReleaseMacroLock();

            // Gıda hazırlık aşaması varsa sevkiyatlar ReadyForDispatch'e HENÜZ alınmaz;
            // MarkFoodPreparationReady tamamlandığında alınacak.
            if (!hasFoodMacroItems)
            {
                var shipments = await _context.WarehouseShipments
                    .Where(s =>
                        s.ZonePreparationId == zp.Id &&
                        s.Status < ShipmentStatus.ReadyForDispatch &&
                        s.Status != ShipmentStatus.Cancelled &&
                        s.Status != ShipmentStatus.Passive
                    )
                    .ToListAsync(cancellationToken);

                foreach (var s in shipments)
                    s.ChangeStatus(ShipmentStatus.ReadyForDispatch, _currentUserService.UserId);
            }

            await _context.SaveChangesAsync(cancellationToken);

            return new MarkZoneMacroReadyResult
            {
                Success                = true,
                UnfilledMacroLineCount = unfilledMacroCount,
                AdvancedToFoodPrep     = hasFoodMacroItems,
            };
        }
    }
}
