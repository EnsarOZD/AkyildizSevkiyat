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
        string? Category
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
            public int? LocalStockId { get; set; }
            public string OriginalStockCode { get; set; } = string.Empty;
            public string ProjectName { get; set; } = string.Empty;
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
                .Include(sl => sl.IssOrderLine) // Ensure included for mapping
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
                   StockCode = sl.IssOrderLine != null ? sl.IssOrderLine.StockCode : sl.StockCode,
                   sl.StockName,
                   sl.Unit,
                   sl.Shipment.ProjectId,
                   ProjectName = sl.Shipment.Project.Name // Retrieve Project Name
                })
                .ToListAsync(cancellationToken);

            // Fetch Mappings
            var externalCodes = lines.Select(l => l.StockCode).Where(x => x != null).Distinct().ToList();
            var mappings = await _context.StockMappings
                .Include(m => m.LocalStock)
                .AsNoTracking()
                .Where(m => externalCodes.Contains(m.ExternalStockCode))
                .ToListAsync(cancellationToken);

            // Prepare Aggregation List
            var rawItems = new List<MacroRawItem>();

            foreach(var line in lines)
            {
                // Safety check for stockcode
                if (string.IsNullOrEmpty(line.StockCode)) continue; 

                var mapping = mappings.FirstOrDefault(m => m.ExternalStockCode == line.StockCode);
                
                bool isMacro = false;
                string finalStockCode = line.StockCode;
                string finalStockName = line.StockName;
                string finalUnit = line.Unit.ToString();
                string? category = null;
                int? localStockId = null;

                if (mapping != null && mapping.LocalStock != null)
                {
                    if (mapping.LocalStock.PickingType == PickingType.Macro) isMacro = true;
                    finalStockCode = mapping.LocalStock.StockCode;
                    finalStockName = mapping.LocalStock.StockName;
                    finalUnit = mapping.LocalStock.Unit.ToString(); // Enum to String
                    category = mapping.LocalStock.Category.ToString();
                    localStockId = mapping.LocalStock.Id;
                }
                
                if (isMacro)
                {
                    rawItems.Add(new MacroRawItem {
                        LineId = line.Id,
                        ProjectId = line.ProjectId,
                        OrderedQty = line.OrderedQty,
                        PickedQty = line.DeliveredQty,
                        StockCode = finalStockCode,
                        StockName = finalStockName,
                        Unit = finalUnit,
                        Category = category,
                        LocalStockId = localStockId,
                        OriginalStockCode = line.StockCode,
                        ProjectName = line.ProjectName
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
                    g.First().Category
                ))
                .OrderBy(r => r.Category).ThenBy(r => r.StockName)
                .ToList();


            return grouped;
        }
    }
}
