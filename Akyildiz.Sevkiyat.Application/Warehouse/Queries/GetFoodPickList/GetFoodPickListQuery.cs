using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Queries.GetFoodPickList
{
    /// <summary>Alt satır — tek bir sevkiyat satırını temsil eder.</summary>
    public record FoodPickSubLineDto(
        int ShipmentLineId,
        int ZonePreparationId,
        int BatchNo,
        int ProjectId,
        string ProjectName,
        decimal OrderedQty,
        decimal PickedQty
    );

    public record FoodPickItemDto(
        List<FoodPickSubLineDto> Lines,
        string StockCode,
        string StockName,
        string Unit,
        decimal TotalOrderedQty,
        decimal TotalPickedQty,
        int ProjectCount,
        int BatchCount,
        bool IsCompleted,
        int PickingOrder,
        /// <summary>Toplam ağırlık (kg). WeightKg tanımlı değilse null.</summary>
        decimal? TotalWeightKg
    );

    /// <summary>
    /// Aynı zone + tarih için GidaHazirlik (veya ReadyForDriverInfo+ ama gıda kalemi toplanmamış)
    /// aşamasındaki tüm batch'lerin gıda macro kalemlerini birleştirir.
    /// </summary>
    public record GetFoodPickListQuery(int ZoneId, DateTime DeliveryDate) : IRequest<List<FoodPickItemDto>>;

    public class GetFoodPickListQueryHandler : IRequestHandler<GetFoodPickListQuery, List<FoodPickItemDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetFoodPickListQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<FoodPickItemDto>> Handle(GetFoodPickListQuery request, CancellationToken cancellationToken)
        {
            var deliveryDateUtc = request.DeliveryDate.Date;

            // Bu zone + tarih için GidaHazirlik aşamasındaki hazırlıkları bul
            var zonePreps = await _context.ZonePreparations
                .Include(z => z.Projects)
                .AsNoTracking()
                .Where(z => z.ZoneId == request.ZoneId
                         && z.DeliveryDate.Date == deliveryDateUtc
                         && z.Status == ZonePreparationStatus.GidaHazirlik)
                .ToListAsync(cancellationToken);

            if (!zonePreps.Any()) return new List<FoodPickItemDto>();

            var zpIds = zonePreps.Select(z => z.Id).ToList();
            var batchNoMap = zonePreps.ToDictionary(z => z.Id, z => z.BatchNo);

            // Tüm batch'lerin shipment line'larını sorgula
            var lines = await _context.ShipmentLines
                .Where(sl =>
                    sl.Shipment.ZonePreparationId != null &&
                    zpIds.Contains(sl.Shipment.ZonePreparationId.Value) &&
                    sl.Shipment.Status != ShipmentStatus.Cancelled &&
                    sl.Shipment.Status != ShipmentStatus.Passive &&
                    sl.OrderedQty > 0)
                .Select(sl => new
                {
                    sl.Id,
                    sl.OrderedQty,
                    sl.DeliveredQty,
                    sl.StockCode,
                    sl.StockName,
                    sl.Unit,
                    sl.StockMasterId,
                    ZonePreparationId = sl.Shipment.ZonePreparationId!.Value,
                    sl.Shipment.ProjectId,
                    ProjectName    = sl.Shipment.Project.Name,
                    PickingType    = sl.StockMaster != null ? (PickingType?)sl.StockMaster.PickingType : null,
                    Category       = sl.StockMaster != null ? (StockCategory?)sl.StockMaster.Category : null,
                    PickingOrder   = sl.StockMaster != null ? sl.StockMaster.PickingOrder : 0,
                    LocalStockCode = sl.StockMaster != null ? sl.StockMaster.StockCode : null,
                    LocalStockName = sl.StockMaster != null ? sl.StockMaster.StockName : null,
                    LocalUnit      = sl.StockMaster != null ? sl.StockMaster.Unit.ToString() : null,
                    WeightKg       = sl.StockMaster != null ? sl.StockMaster.WeightKg : null,
                    ExternalCode   = sl.IssOrderLine != null ? sl.IssOrderLine.StockCode : sl.StockCode,
                })
                .ToListAsync(cancellationToken);

            // Doğrudan StockMaster bağlantısı olmayan kodlar için mapping bak
            var needsMappingCodes = lines
                .Where(l => !l.StockMasterId.HasValue)
                .Select(l => l.ExternalCode)
                .Distinct()
                .ToList();

            var mappings = needsMappingCodes.Any()
                ? await _context.StockMappings
                    .Include(m => m.LocalStock)
                    .AsNoTracking()
                    .Where(m => needsMappingCodes.Contains(m.ExternalStockCode))
                    .ToListAsync(cancellationToken)
                : new List<Domain.Entities.StockMapping>();

            // Sadece gıda + macro kalemlerini filtrele
            var rawItems = new List<(
                int LineId, int ZpId, int BatchNo, int ProjectId, string ProjectName,
                decimal OrderedQty, decimal PickedQty,
                string StockCode, string StockName, string Unit,
                int? LocalStockId, string GroupKey,
                int PickingOrder, decimal? WeightKg)>();

            foreach (var line in lines)
            {
                bool isMacro = false;
                bool isGida  = false;
                string finalStockCode = line.StockCode;
                string finalStockName = line.StockName;
                string finalUnit      = line.Unit.ToString();
                int? localStockId     = line.StockMasterId;
                string groupKey;
                int pickingOrder = line.PickingOrder;

                decimal? weightKg = line.WeightKg;

                if (line.StockMasterId.HasValue)
                {
                    isMacro = line.PickingType == PickingType.Macro;
                    isGida  = line.Category == StockCategory.Gida;
                    finalStockCode = line.LocalStockCode ?? line.StockCode;
                    finalStockName = line.LocalStockName ?? line.StockName;
                    finalUnit      = line.LocalUnit ?? line.Unit.ToString();
                    groupKey       = $"L-{line.StockMasterId}";
                }
                else
                {
                    var mapping = mappings.FirstOrDefault(m =>
                        m.ExternalStockCode.Equals(line.ExternalCode, StringComparison.OrdinalIgnoreCase));
                    if (mapping?.LocalStock != null)
                    {
                        isMacro = mapping.LocalStock.PickingType == PickingType.Macro;
                        isGida  = mapping.LocalStock.Category == StockCategory.Gida;
                        finalStockCode = mapping.LocalStock.StockCode;
                        finalStockName = mapping.LocalStock.StockName;
                        finalUnit      = mapping.LocalStock.Unit.ToString();
                        pickingOrder   = mapping.LocalStock.PickingOrder;
                        localStockId   = mapping.LocalStock.Id;
                        weightKg       = mapping.LocalStock.WeightKg;
                        groupKey       = $"L-{mapping.LocalStock.Id}";
                    }
                    else
                    {
                        groupKey = $"E-{line.ExternalCode}";
                    }
                }

                if (!isMacro || !isGida) continue;

                rawItems.Add((
                    line.Id,
                    line.ZonePreparationId,
                    batchNoMap.GetValueOrDefault(line.ZonePreparationId, 1),
                    line.ProjectId,
                    line.ProjectName,
                    line.OrderedQty,
                    line.DeliveredQty,
                    finalStockCode,
                    finalStockName,
                    finalUnit,
                    localStockId,
                    groupKey,
                    pickingOrder,
                    weightKg
                ));
            }

            var grouped = rawItems
                .GroupBy(x => x.GroupKey)
                .Select(g =>
                {
                    var first = g.First();
                    var totalOrdered = g.Sum(x => x.OrderedQty);
                    var totalPicked  = g.Sum(x => x.PickedQty);
                    // Ağırlık: aynı stok kodu aynı WeightKg'ye sahip olacak, qty ile çarp
                    decimal? totalWeight = first.WeightKg.HasValue
                        ? g.Sum(x => x.WeightKg.GetValueOrDefault() * x.OrderedQty)
                        : (decimal?)null;

                    return new FoodPickItemDto(
                        Lines: g.Select(x => new FoodPickSubLineDto(
                            x.LineId, x.ZpId, x.BatchNo, x.ProjectId, x.ProjectName, x.OrderedQty, x.PickedQty
                        )).ToList(),
                        StockCode:      first.StockCode,
                        StockName:      first.StockName,
                        Unit:           first.Unit,
                        TotalOrderedQty: totalOrdered,
                        TotalPickedQty:  totalPicked,
                        ProjectCount:   g.Select(x => x.ProjectId).Distinct().Count(),
                        BatchCount:     g.Select(x => x.ZpId).Distinct().Count(),
                        IsCompleted:    totalPicked >= totalOrdered && totalPicked > 0,
                        PickingOrder:   first.PickingOrder,
                        TotalWeightKg:  totalWeight
                    );
                })
                .OrderBy(r => r.PickingOrder)
                .ThenBy(r => r.StockName)
                .ToList();

            return grouped;
        }
    }
}
