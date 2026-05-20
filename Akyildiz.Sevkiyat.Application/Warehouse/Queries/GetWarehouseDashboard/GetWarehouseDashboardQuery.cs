using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.Warehouse.Queries.GetZonePreparationStatus; // Reuse DTOs if possible, or redefine? Let's check.
// Actually better to define the DTOs here or shared. 
// For now, I will reuse the DTO classes if they are public.
// But they might be nested or in the same namespace. 
// Let's redefine DTOs here to be self-contained or use the existing ones if they are in a separate file.
// Checking previous view_file: ZonePreparationDto was correct.
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Queries.GetWarehouseDashboard
{
    // Reuse DTOs from GetZonePreparationStatus namespace to avoid duplication if they match structure.
    // However, clean architecture suggests DTOs specific to the query or shared.
    // Since the structure is identical (List of Zones), let's just use the same DTO logic.
    // I will redefine them here for clarity and independence.

    public record DashboardZoneDto(
        int Id,
        int ZoneId,
        string ZoneName,
        int BatchNo,
        string BatchLabel,
        DateTime DeliveryDate,
        string Status,
        int StatusId,
        bool IsFrozen,
        bool IrsaliyeFetched,
        List<DashboardProjectDto> Projects,
        int OpenErrorCount,
        int OpenWarningCount,
        bool IsOutOfCity,
        string? MacroLockedByUserName,
        DateTime? MacroLockedAt,
        /// <summary>Gıda macro kalemlerin toplam ağırlığı (kg). Status 4-5 için hesaplanır, WeightKg girilmemişse null.</summary>
        decimal? FoodTotalWeightKg,
        /// <summary>Gıda macro kalemlerin toplanan ağırlığı (kg). PickedQty * WeightKg toplamı.</summary>
        decimal? FoodPickedWeightKg
    );

    public record DashboardProjectDto(
        int Id,
        int ProjectId,
        string ProjectCode,
        string ProjectName,
        bool IsMicroReady,
        DateTime? MicroReadyAt,
        bool IsAddedLater,
        string? PreparedByUserName,
        string? PickingLockedByUserName,
        DateTime? PickingLockedAt,
        /// <summary>Bu projenin zone'undaki aktif sevkiyat ID'si. Depo teslim için kullanılır.</summary>
        int? ShipmentId,
        /// <summary>Bu projenin ait olduğu ZonePreparation.Id (birleştirilmiş görünümde proje bazlı işlem için gerekli).</summary>
        int ZonePreparationId
    );

    public record GetWarehouseDashboardQuery : IRequest<List<DashboardZoneDto>>;

    public class GetWarehouseDashboardQueryHandler : IRequestHandler<GetWarehouseDashboardQuery, List<DashboardZoneDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetWarehouseDashboardQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<DashboardZoneDto>> Handle(GetWarehouseDashboardQuery request, CancellationToken cancellationToken)
        {
            // Tüm aktif (Dispatched olmayan) zone hazırlıklarını getir — tarih filtresiz
            var existingPreps = await _context.ZonePreparations
                .Include(z => z.Zone)
                .Include(z => z.Projects)
                    .ThenInclude(p => p.Project)
                .Where(z => z.Status != ZonePreparationStatus.Dispatched)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            // 2. Load reconciliation issue counts for all relevant zone preps in one query
            var prepIds = existingPreps.Select(p => p.Id).ToList();

            // Defansif: ZP henüz Dispatched'e geçmemiş ama içindeki sevkiyat
            // dispatched/delivered/returned olabilir (örn. mixed-state batch). Bunları
            // proje haritasından dışla — depo hazırlığı pipeline'ı dışındalar.
            var shipmentZoneMap = await _context.WarehouseShipments
                .Where(s => s.ZonePreparationId != null && prepIds.Contains(s.ZonePreparationId.Value)
                         && s.Status != ShipmentStatus.Cancelled
                         && s.Status != ShipmentStatus.Passive
                         && s.Status != ShipmentStatus.Dispatched
                         && s.Status != ShipmentStatus.Delivered
                         && s.Status != ShipmentStatus.ReturnedToWarehouse)
                .Select(s => new { s.Id, ZonePreparationId = s.ZonePreparationId!.Value, s.ProjectId })
                .ToListAsync(cancellationToken);

            var shipmentIds = shipmentZoneMap.Select(s => s.Id).ToList();

            // Proje bazında sevkiyat ID haritası: (ZonePreparationId, ProjectId) → ShipmentId
            var shipmentByZoneProject = shipmentZoneMap
                .GroupBy(s => (s.ZonePreparationId, s.ProjectId))
                .ToDictionary(g => g.Key, g => g.First().Id);

            // Gıda + macro kalemlerin toplam ağırlığı (sadece status 4-5 zone'lar için)
            var foodWeightZoneIds = existingPreps
                .Where(p => p.Status == ZonePreparationStatus.GidaHazirlik ||
                            p.Status == ZonePreparationStatus.ReadyForDriverInfo)
                .Select(p => p.Id)
                .ToList();

            var foodWeightByZone       = new Dictionary<int, decimal>();
            var foodPickedWeightByZone = new Dictionary<int, decimal>();
            if (foodWeightZoneIds.Any())
            {
                var foodWeightData = await _context.ShipmentLines
                    .Where(sl =>
                        sl.Shipment.ZonePreparationId != null &&
                        foodWeightZoneIds.Contains(sl.Shipment.ZonePreparationId.Value) &&
                        sl.Shipment.Status != ShipmentStatus.Cancelled &&
                        sl.Shipment.Status != ShipmentStatus.Passive &&
                        sl.StockMaster != null &&
                        sl.StockMaster.Category == StockCategory.Gida &&
                        sl.StockMaster.PickingType == PickingType.Macro &&
                        sl.StockMaster.WeightKg != null &&
                        sl.OrderedQty > 0)
                    .Select(sl => new
                    {
                        ZonePreparationId = sl.Shipment.ZonePreparationId!.Value,
                        WeightKg = sl.StockMaster!.WeightKg!.Value,
                        sl.OrderedQty,
                        PickedQty = sl.DeliveredQty,
                    })
                    .ToListAsync(cancellationToken);

                foodWeightByZone = foodWeightData
                    .GroupBy(x => x.ZonePreparationId)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.WeightKg * x.OrderedQty));

                foodPickedWeightByZone = foodWeightData
                    .GroupBy(x => x.ZonePreparationId)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.WeightKg * x.PickedQty));
            }

            var errorsByZone  = new Dictionary<int, int>();
            var warnsByZone   = new Dictionary<int, int>();

            if (shipmentIds.Count > 0)
            {
                var openIssues = await _context.ReconciliationIssues
                    .Where(i => i.ShipmentId != null
                             && shipmentIds.Contains(i.ShipmentId.Value)
                             && i.Status == ReconciliationStatus.Open)
                    .Select(i => new { i.ShipmentId, i.Severity })
                    .ToListAsync(cancellationToken);

                var joined = openIssues
                    .Join(shipmentZoneMap, i => i.ShipmentId, s => s.Id, (i, s) => new { s.ZonePreparationId, i.Severity })
                    .ToList();

                errorsByZone = joined
                    .Where(x => x.Severity == ReconciliationSeverity.Error)
                    .GroupBy(x => x.ZonePreparationId)
                    .ToDictionary(g => g.Key, g => g.Count());

                warnsByZone = joined
                    .Where(x => x.Severity == ReconciliationSeverity.Warning)
                    .GroupBy(x => x.ZonePreparationId)
                    .ToDictionary(g => g.Key, g => g.Count());
            }

            // 3. Build Result
            var result = new List<DashboardZoneDto>();

            foreach (var prep in existingPreps.OrderBy(p => p.Zone?.Name).ThenBy(p => p.BatchNo))
            {
                // Filtering:
                // 1. Skip if Status is ReadyForTransfer (5) -> User requested to hide completed.
                // 2. Skip if NO Projects (Empty Batch)
                if (prep.Status != ZonePreparationStatus.Dispatched &&
                    prep.Projects.Any())
                {
                    string label = prep.BatchNo == 1 ? "Ana Toplama" : $"Geç Gelenler (Batch {prep.BatchNo})";

                    var foodWeight       = foodWeightByZone.TryGetValue(prep.Id, out var fw) ? fw : (decimal?)null;
                    var foodPickedWeight = foodPickedWeightByZone.TryGetValue(prep.Id, out var fpw) ? fpw : (decimal?)null;

                    result.Add(new DashboardZoneDto(
                        prep.Id,
                        prep.ZoneId,
                        prep.Zone?.Name ?? "Unknown Zone",
                        prep.BatchNo,
                        label,
                        prep.DeliveryDate,
                        prep.Status.ToString(),
                        (int)prep.Status,
                        prep.IsFrozen,
                        prep.IrsaliyeFetched,
                        prep.Projects.Select(p => new DashboardProjectDto(
                            p.Id,
                            p.ProjectId,
                            p.Project?.Code ?? "N/A",
                            p.Project?.Name ?? "Unknown Project",
                            p.IsMicroReady,
                            p.MicroReadyAt,
                            p.IsAddedLater,
                            p.PreparedByUserName,
                            p.PickingLockedByUserName,
                            p.PickingLockedAt,
                            shipmentByZoneProject.TryGetValue((prep.Id, p.ProjectId), out var sid) ? sid : (int?)null,
                            prep.Id
                        )).ToList(),
                        errorsByZone.GetValueOrDefault(prep.Id, 0),
                        warnsByZone.GetValueOrDefault(prep.Id, 0),
                        prep.Zone?.IsOutOfCity ?? false,
                        prep.MacroLockedByUserName,
                        prep.MacroLockedAt,
                        foodWeight,
                        foodPickedWeight
                    ));
                }
            }

            return result;
        }
    }
}
