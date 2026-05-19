using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.MarkFoodPreparationReady
{
    public record MarkFoodPreparationReadyCommand : IRequest<MarkFoodPreparationReadyResult>, IRequireRoles
    {
        public int ZoneId { get; init; }
        public DateTime DeliveryDate { get; init; }
        public bool ForceComplete { get; init; } = false;
        public string? ForceReason { get; init; }

        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting", "Warehouse", "Driver" };
    }

    public class MarkFoodPreparationReadyResult
    {
        public bool Success { get; set; }
        public int AdvancedBatchCount { get; set; }
        public int UnfilledFoodLineCount { get; set; }
    }

    public class MarkFoodPreparationReadyCommandHandler
        : IRequestHandler<MarkFoodPreparationReadyCommand, MarkFoodPreparationReadyResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public MarkFoodPreparationReadyCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<MarkFoodPreparationReadyResult> Handle(
            MarkFoodPreparationReadyCommand request,
            CancellationToken cancellationToken)
        {
            var deliveryDateUtc = request.DeliveryDate.Date;

            // Bu zone + tarih için GidaHazirlik aşamasındaki tüm batch'leri bul
            var gidaZones = await _context.ZonePreparations
                .Include(z => z.Projects)
                .Where(z => z.ZoneId == request.ZoneId
                         && z.DeliveryDate.Date == deliveryDateUtc
                         && z.Status == ZonePreparationStatus.GidaHazirlik)
                .ToListAsync(cancellationToken);

            if (!gidaZones.Any())
                throw new DomainException("Bu zone + tarih için gıda hazırlık aşamasında batch bulunamadı.");

            if (request.ForceComplete && string.IsNullOrWhiteSpace(request.ForceReason))
                throw new DomainException("ForceComplete=true kullanılırken ForceReason zorunludur.");

            var zpIds = gidaZones.Select(z => z.Id).ToList();

            // Gıda macro satırlarının doluluk kontrolü
            var foodLinesData = await _context.ShipmentLines
                .Where(l =>
                    l.Shipment.ZonePreparationId != null &&
                    zpIds.Contains(l.Shipment.ZonePreparationId.Value) &&
                    l.Shipment.Status != ShipmentStatus.Cancelled &&
                    l.Shipment.Status != ShipmentStatus.Passive &&
                    l.OrderedQty > 0)
                .Select(l => new
                {
                    l.Id,
                    l.DeliveredQty,
                    l.StockMasterId,
                    StockCode   = l.IssOrderLine != null ? l.IssOrderLine.StockCode : l.StockCode,
                    PickingType = l.StockMaster != null ? (PickingType?)l.StockMaster.PickingType : null,
                    Category    = l.StockMaster != null ? (StockCategory?)l.StockMaster.Category : null,
                })
                .ToListAsync(cancellationToken);

            var externalCodes = foodLinesData.Where(l => !l.StockMasterId.HasValue)
                                             .Select(l => l.StockCode).Distinct().ToList();

            var mappings = externalCodes.Any()
                ? await _context.StockMappings
                    .Include(m => m.LocalStock)
                    .Where(m => externalCodes.Contains(m.ExternalStockCode))
                    .ToListAsync(cancellationToken)
                : new List<Domain.Entities.StockMapping>();

            int unfilledCount = 0;
            foreach (var line in foodLinesData)
            {
                bool isGidaMacro = false;

                if (line.StockMasterId.HasValue)
                {
                    isGidaMacro = line.PickingType == PickingType.Macro
                               && line.Category == StockCategory.Gida;
                }
                else
                {
                    var mapping = mappings.FirstOrDefault(m =>
                        m.ExternalStockCode.Equals(line.StockCode, StringComparison.OrdinalIgnoreCase));
                    if (mapping?.LocalStock != null)
                        isGidaMacro = mapping.LocalStock.PickingType == PickingType.Macro
                                   && mapping.LocalStock.Category == StockCategory.Gida;
                }

                if (isGidaMacro && line.DeliveredQty == 0)
                    unfilledCount++;
            }

            if (unfilledCount > 0 && !request.ForceComplete)
                throw new DomainException(
                    $"{unfilledCount} gıda satırı henüz toplanmadı. " +
                    "Eksik satırlarla devam etmek için ForceComplete=true ve ForceReason gönderin.");

            // Tüm GidaHazirlik batch'lerini ReadyForDriverInfo'ya ilerlet
            foreach (var zp in gidaZones)
            {
                zp.Status = ZonePreparationStatus.ReadyForDriverInfo;
            }

            // Gıda hazırlığı tamamlandı — sevkiyatları ReadyForDispatch'e al
            var shipmentsToAdvance = await _context.WarehouseShipments
                .Where(s =>
                    s.ZonePreparationId != null &&
                    zpIds.Contains(s.ZonePreparationId.Value) &&
                    s.Status < ShipmentStatus.ReadyForDispatch &&
                    s.Status != ShipmentStatus.Cancelled &&
                    s.Status != ShipmentStatus.Passive
                )
                .ToListAsync(cancellationToken);

            foreach (var s in shipmentsToAdvance)
                s.ChangeStatus(ShipmentStatus.ReadyForDispatch, _currentUserService.UserId);

            await _context.SaveChangesAsync(cancellationToken);

            return new MarkFoodPreparationReadyResult
            {
                Success              = true,
                AdvancedBatchCount   = gidaZones.Count,
                UnfilledFoodLineCount = unfilledCount,
            };
        }
    }
}
