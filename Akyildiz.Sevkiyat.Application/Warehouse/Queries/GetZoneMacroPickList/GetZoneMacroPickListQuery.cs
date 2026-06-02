using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Queries.GetZoneMacroPickList
{
    public record MacroSubLineDto(int Id, int ProjectId, string ProjectName, decimal OrderedQty);

    public record MacroPickItemDto(
        List<MacroSubLineDto> Lines,
        string StockCode,
        string StockName,
        string Unit,
        decimal TotalOrderedQty,
        decimal TotalPickedQty,
        int ProjectCount,
        bool IsCompleted,
        string? Category,
        int PickingOrder,
        string? DifferenceReason
    );

    public record GetZoneMacroPickListQuery(int ZonePreparationId) : IRequest<List<MacroPickItemDto>>;

    public class GetZoneMacroPickListQueryHandler : IRequestHandler<GetZoneMacroPickListQuery, List<MacroPickItemDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetZoneMacroPickListQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        private class MacroRawItem
        {
            public int LineId { get; set; }
            public int ProjectId { get; set; }
            public decimal OrderedQty { get; set; }
            public decimal PickedQty { get; set; }
            public string StockCode { get; set; } = string.Empty;
            public string StockName { get; set; } = string.Empty;
            public string Unit { get; set; } = string.Empty;
            public string? Category { get; set; }
            public int PickingOrder { get; set; }
            public int? LocalStockId { get; set; }
            public string OriginalStockCode { get; set; } = string.Empty;
            public string ProjectName { get; set; } = string.Empty;
            public string? DifferenceReason { get; set; }
        }

        public async Task<List<MacroPickItemDto>> Handle(GetZoneMacroPickListQuery request, CancellationToken cancellationToken)
        {
            var zp = await _context.ZonePreparations
                .Include(z => z.Projects)
                .AsNoTracking()
                .FirstOrDefaultAsync(z => z.Id == request.ZonePreparationId, cancellationToken);

            if (zp == null) return new List<MacroPickItemDto>();

            var projectIds = zp.Projects.Select(p => p.ProjectId).ToList();

            // Get all shipment lines
            var lines = await _context.ShipmentLines
                .Include(sl => sl.Shipment)
                .Include(sl => sl.IssOrderLine) 
                .Include(sl => sl.StockMaster)
                .Where(sl => 
                    projectIds.Contains(sl.Shipment.ProjectId) && 
                    sl.Shipment.ZonePreparationId == zp.Id && 
                    sl.Shipment.Status != ShipmentStatus.Cancelled &&
                    sl.Shipment.Status != ShipmentStatus.Passive
                )
                .Select(sl => new
                {
                    sl.Id,
                    sl.OrderedQty,
                    sl.DeliveredQty,
                    sl.DifferenceReason,
                    // ISS kodu sadece doğrudan StockMaster bağlantısı yoksa kullan
                    ExternalStockCode = sl.IssOrderLine != null ? sl.IssOrderLine.StockCode : sl.StockCode,
                    sl.StockCode,
                    sl.StockName,
                    sl.Unit,
                    sl.Shipment.ProjectId,
                    ProjectName = sl.Shipment.Project.Name,
                    sl.StockMasterId,
                    PickingType    = sl.StockMaster != null ? (PickingType?)sl.StockMaster.PickingType : null,
                    LocalStockCode = sl.StockMaster != null ? sl.StockMaster.StockCode : null,
                    LocalStockName = sl.StockMaster != null ? sl.StockMaster.StockName : null,
                    LocalUnit      = sl.StockMaster != null ? sl.StockMaster.Unit.ToString() : null,
                    LocalCategory  = sl.StockMaster != null ? sl.StockMaster.Category.ToString() : null,
                    LocalPickingOrder = sl.StockMaster != null ? sl.StockMaster.PickingOrder : 0,
                })
                .ToListAsync(cancellationToken);

            // Fetch Mappings — sadece doğrudan StockMaster bağlantısı olmayan kalemler için
            var needsMappingCodes = lines
                .Where(l => !l.StockMasterId.HasValue)
                .Select(l => l.ExternalStockCode)
                .Where(x => x != null)
                .Distinct()
                .ToList();

            var mappings = await _context.StockMappings
                .Include(m => m.LocalStock)
                .AsNoTracking()
                .Where(m => needsMappingCodes.Contains(m.ExternalStockCode))
                .ToListAsync(cancellationToken);

            var rawItems = new List<MacroRawItem>();

            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line.ExternalStockCode) && string.IsNullOrEmpty(line.StockCode)) continue;

                bool isMacro = false;
                string finalStockCode = line.StockCode;
                string finalStockName = line.StockName;
                string finalUnit = line.Unit.ToString();
                string? category = null;
                int? localStockId = line.StockMasterId;

                int pickingOrder = 0;

                if (line.StockMasterId.HasValue)
                {
                    // Doğrudan StockMaster bağlantısı: her zaman öncelikli
                    if (line.PickingType == PickingType.Macro) isMacro = true;
                    finalStockCode = line.LocalStockCode ?? line.StockCode;
                    finalStockName = line.LocalStockName ?? line.StockName;
                    finalUnit      = line.LocalUnit ?? line.Unit.ToString();
                    category       = line.LocalCategory;
                    pickingOrder   = line.LocalPickingOrder;
                }
                else
                {
                    var mapping = mappings.FirstOrDefault(m =>
                        m.ExternalStockCode.Equals(line.ExternalStockCode, System.StringComparison.OrdinalIgnoreCase));

                    if (mapping?.LocalStock != null)
                    {
                        if (mapping.LocalStock.PickingType == PickingType.Macro) isMacro = true;
                        finalStockCode = mapping.LocalStock.StockCode;
                        finalStockName = mapping.LocalStock.StockName;
                        finalUnit      = mapping.LocalStock.Unit.ToString();
                        category       = mapping.LocalStock.Category.ToString();
                        pickingOrder   = mapping.LocalStock.PickingOrder;
                        localStockId   = mapping.LocalStock.Id;
                    }
                }

                // Gıda ürünleri GidaHazirlik aşamasında ayrı toplanır — macro listesine dahil etme
                if (isMacro && category != StockCategory.Gida.ToString())
                {
                    rawItems.Add(new MacroRawItem
                    {
                        LineId            = line.Id,
                        ProjectId         = line.ProjectId,
                        OrderedQty        = line.OrderedQty,
                        PickedQty         = line.DeliveredQty,
                        StockCode         = finalStockCode,
                        StockName         = finalStockName,
                        Unit              = finalUnit,
                        Category          = category,
                        PickingOrder      = pickingOrder,
                        LocalStockId      = localStockId,
                        OriginalStockCode = line.ExternalStockCode,
                        ProjectName       = line.ProjectName,
                        DifferenceReason  = line.DifferenceReason
                    });
                }
            }

            var grouped = rawItems
                .GroupBy(x => x.LocalStockId.HasValue ? $"L-{x.LocalStockId}" : $"E-{x.OriginalStockCode}")
                .Select(g => new MacroPickItemDto(
                    g.Select(x => new MacroSubLineDto(x.LineId, x.ProjectId, x.ProjectName, x.OrderedQty)).ToList(),
                    g.First().StockCode,
                    g.First().StockName,
                    g.First().Unit,
                    g.Sum(x => x.OrderedQty),
                    g.Sum(x => x.PickedQty),
                    g.Select(x => x.ProjectId).Distinct().Count(),
                    g.Sum(x => x.PickedQty) >= g.Sum(x => x.OrderedQty),
                    g.First().Category,
                    g.First().PickingOrder,
                    g.Select(x => x.DifferenceReason).FirstOrDefault(r => !string.IsNullOrWhiteSpace(r))
                ))
                .OrderBy(r => r.Category).ThenBy(r => r.PickingOrder).ThenBy(r => r.StockName)
                .ToList();


            return grouped;
        }
    }
}
