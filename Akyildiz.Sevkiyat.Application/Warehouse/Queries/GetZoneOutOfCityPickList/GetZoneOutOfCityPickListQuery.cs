using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Queries.GetZoneOutOfCityPickList
{
    public record OutOfCitySubLineDto(int ShipmentLineId, int ProjectId, string ProjectName, decimal OrderedQty);

    public record OutOfCityPickItemDto(
        List<OutOfCitySubLineDto> Lines,
        string StockCode,
        string StockName,
        string Unit,
        decimal TotalOrderedQty,
        decimal TotalPickedQty,
        bool IsCompleted,
        string? Category,
        string? PickingType
    );

    public record GetZoneOutOfCityPickListQuery(int ZonePreparationId, int? ProjectId = null) : IRequest<List<OutOfCityPickItemDto>>;

    public class GetZoneOutOfCityPickListQueryHandler : IRequestHandler<GetZoneOutOfCityPickListQuery, List<OutOfCityPickItemDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetZoneOutOfCityPickListQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        private class RawItem
        {
            public int LineId { get; set; }
            public int ProjectId { get; set; }
            public string ProjectName { get; set; } = string.Empty;
            public decimal OrderedQty { get; set; }
            public decimal PickedQty { get; set; }
            public string StockCode { get; set; } = string.Empty;
            public string StockName { get; set; } = string.Empty;
            public string Unit { get; set; } = string.Empty;
            public string? Category { get; set; }
            public PickingType? PickingType { get; set; }
            public int? LocalStockId { get; set; }
            public string OriginalStockCode { get; set; } = string.Empty;
        }

        public async Task<List<OutOfCityPickItemDto>> Handle(GetZoneOutOfCityPickListQuery request, CancellationToken cancellationToken)
        {
            var zp = await _context.ZonePreparations
                .Include(z => z.Projects)
                .AsNoTracking()
                .FirstOrDefaultAsync(z => z.Id == request.ZonePreparationId, cancellationToken);

            if (zp == null) return new List<OutOfCityPickItemDto>();

            var projectIds = request.ProjectId.HasValue
                ? new List<int> { request.ProjectId.Value }
                : zp.Projects.Select(p => p.ProjectId).ToList();

            var lines = await _context.ShipmentLines
                .Where(sl =>
                    projectIds.Contains(sl.Shipment.ProjectId) &&
                    sl.Shipment.ZonePreparationId == zp.Id &&
                    sl.Shipment.Status != ShipmentStatus.Cancelled &&
                    sl.Shipment.Status != ShipmentStatus.Passive &&
                    sl.OrderedQty > 0)
                .Select(sl => new
                {
                    sl.Id,
                    sl.OrderedQty,
                    sl.DeliveredQty,
                    ExternalStockCode = sl.IssOrderLine != null ? sl.IssOrderLine.StockCode : sl.StockCode,
                    sl.StockCode,
                    sl.StockName,
                    sl.Unit,
                    sl.Shipment.ProjectId,
                    ProjectName    = sl.Shipment.Project.Name,
                    sl.StockMasterId,
                    PickingType    = sl.StockMaster != null ? (PickingType?)sl.StockMaster.PickingType : null,
                    LocalStockCode = sl.StockMaster != null ? sl.StockMaster.StockCode : null,
                    LocalStockName = sl.StockMaster != null ? sl.StockMaster.StockName : null,
                    LocalUnit      = sl.StockMaster != null ? sl.StockMaster.Unit.ToString() : null,
                    LocalCategory  = sl.StockMaster != null ? sl.StockMaster.Category.ToString() : null,
                })
                .ToListAsync(cancellationToken);

            var needsMappingCodes = lines
                .Where(l => !l.StockMasterId.HasValue)
                .Select(l => l.ExternalStockCode)
                .Distinct()
                .ToList();

            var mappings = await _context.StockMappings
                .Include(m => m.LocalStock)
                .AsNoTracking()
                .Where(m => needsMappingCodes.Contains(m.ExternalStockCode))
                .ToListAsync(cancellationToken);

            var rawItems = new List<RawItem>();

            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line.ExternalStockCode) && string.IsNullOrEmpty(line.StockCode)) continue;

                string finalStockCode = line.StockCode;
                string finalStockName = line.StockName;
                string finalUnit      = line.Unit.ToString();
                string? category      = null;
                int? localStockId     = line.StockMasterId;
                PickingType? pickingType = line.PickingType;

                if (line.StockMasterId.HasValue)
                {
                    // Doğrudan StockMaster bağlantısı öncelikli
                    finalStockCode = line.LocalStockCode ?? line.StockCode;
                    finalStockName = line.LocalStockName ?? line.StockName;
                    finalUnit      = line.LocalUnit ?? line.Unit.ToString();
                    category       = line.LocalCategory;
                }
                else
                {
                    var mapping = mappings.FirstOrDefault(m =>
                        m.ExternalStockCode.Equals(line.ExternalStockCode, System.StringComparison.OrdinalIgnoreCase));

                    if (mapping?.LocalStock != null)
                    {
                        finalStockCode = mapping.LocalStock.StockCode;
                        finalStockName = mapping.LocalStock.StockName;
                        finalUnit      = mapping.LocalStock.Unit.ToString();
                        category       = mapping.LocalStock.Category.ToString();
                        localStockId   = mapping.LocalStock.Id;
                        pickingType    = (PickingType?)mapping.LocalStock.PickingType;
                    }
                }

                rawItems.Add(new RawItem
                {
                    LineId = line.Id,
                    ProjectId = line.ProjectId,
                    ProjectName = line.ProjectName,
                    OrderedQty = line.OrderedQty,
                    PickedQty = line.DeliveredQty,
                    StockCode = finalStockCode,
                    StockName = finalStockName,
                    Unit = finalUnit,
                    Category = category,
                    PickingType = pickingType,
                    LocalStockId = localStockId,
                    OriginalStockCode = line.StockCode
                });
            }

            var grouped = rawItems
                .GroupBy(x => x.LocalStockId.HasValue ? $"L-{x.LocalStockId}" : $"E-{x.OriginalStockCode}")
                .Select(g => new OutOfCityPickItemDto(
                    g.Select(x => new OutOfCitySubLineDto(x.LineId, x.ProjectId, x.ProjectName, x.OrderedQty)).ToList(),
                    g.First().StockCode,
                    g.First().StockName,
                    g.First().Unit,
                    g.Sum(x => x.OrderedQty),
                    g.Sum(x => x.PickedQty),
                    g.Sum(x => x.PickedQty) >= g.Sum(x => x.OrderedQty),
                    g.First().Category,
                    g.First().PickingType?.ToString()
                ))
                .OrderBy(r => r.Category).ThenBy(r => r.StockName)
                .ToList();

            return grouped;
        }
    }
}
