using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Queries.GetProjectMicroPickList
{
    public record MicroPickItemDto(
        int ShipmentLineId, // Added for corrections
        string StockCode,
        string StockName,
        string Unit,
        decimal TotalQty,
        decimal PickedQty, // Placeholder for future partial picking
        bool IsCompleted
    );

    public record GetProjectMicroPickListQuery(int ZonePreparationProjectId) : IRequest<List<MicroPickItemDto>>;

    public class GetProjectMicroPickListQueryHandler : IRequestHandler<GetProjectMicroPickListQuery, List<MicroPickItemDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetProjectMicroPickListQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<MicroPickItemDto>> Handle(GetProjectMicroPickListQuery request, CancellationToken cancellationToken)
        {
            // 1. Get Project & Date Info
            var zpProject = await _context.ZonePreparationProjects
                .Include(p => p.ZonePreparation)
                .Include(p => p.Project)
                .FirstOrDefaultAsync(p => p.Id == request.ZonePreparationProjectId, cancellationToken);
            
            if (zpProject == null) return new List<MicroPickItemDto>();

            var deliveryDate = zpProject.ZonePreparation.DeliveryDate;
            var projectId = zpProject.ProjectId;

            // 2. Get Shipment Lines
            // Filter: Project, Date, Stock.PickingType == Micro
            
            var lines = await _context.ShipmentLines
                .Include(sl => sl.Shipment)
                .Include(sl => sl.IssOrderLine) // to get stock mapping logic if needed? No, ShipmentLine has LocalStockId ideally.
                // Wait, ShipmentLine structure:
                // It has IssOrderLineId. 
                // We should look at IssOrderLine -> StockMapping -> StockMaster to get PickingType?
                // OR ShipmentLine has LocalStockId? 
                // checking ShipmentLine.cs...
                // ShipmentLine has StockName, StockCode from IssOrderLine usually.
                // But we need the LOCAL StockMaster to check PickingType.
                // IssOrderLine has StockCode (External). We mapped it to LocalStock via StockMapping.
                // We need to join with StockMapping and StockMaster.
                .Where(sl => 
                    sl.Shipment.ProjectId == projectId && 
                    sl.Shipment.ZonePreparationId == zpProject.ZonePreparationId && // Added Filter
                    sl.Shipment.Status != ShipmentStatus.Cancelled &&
                    sl.Shipment.Status != ShipmentStatus.Passive &&
                    sl.Shipment.Status != ShipmentStatus.Created // Exclude Drafts
                )
                .Select(sl => new 
                {
                   sl.Id,
                   sl.OrderedQty,
                   sl.DeliveredQty, // Added
                   sl.StockCode, // External Code
                   sl.StockName,
                   sl.Unit
                })
                .ToListAsync(cancellationToken);

            // 3. We map External Codes and Get Items
            // We NO LONGER GROUP by Stock. We list individual lines to allow editing each line individually.
            // This is safer for "Exchange Product" scenarios.
            
            var externalCodes = lines.Select(l => l.StockCode).Distinct().ToList();
            
            var mappings = await _context.StockMappings
                .Include(m => m.LocalStock)
                .Where(m => externalCodes.Contains(m.ExternalStockCode))
                .ToListAsync(cancellationToken);

            var result = new List<MicroPickItemDto>();
            
            foreach(var line in lines)
            {
                var mapping = mappings.FirstOrDefault(m => m.ExternalStockCode == line.StockCode);
                
                // Determine Micro/Macro based on mapping
                bool isMicro = false;
                string stockCode = line.StockCode;
                string stockName = line.StockName; // Default to line (ISS) name
                string unit = line.Unit.ToString();

                if (mapping != null && mapping.LocalStock != null)
                {
                    if (mapping.LocalStock.PickingType == PickingType.Micro) isMicro = true;
                    stockCode = mapping.LocalStock.StockCode; // Show Local Code
                    stockName = mapping.LocalStock.StockName; // Show Local Name
                    unit = mapping.LocalStock.Unit.ToString(); // Enum to String
                }
                else 
                {
                    // Strictly exclude from Micro if not mapped to a Micro stock
                    // This satisfies item 1: "Micro toplama ekranında macro olan ürünlerde gözüküyor"
                    // If no mapping, we might still want to see it but let's assume if it's not explicitly Micro, it's not Micro.
                    isMicro = false; 
                }

                if (isMicro)
                {
                    // IsCompleted: DeliveredQty set edilmişse tamamlandı.
                    // Fazla toplama (DeliveredQty > OrderedQty) da tamamlandı sayılır.
                    bool isCompleted = line.DeliveredQty > 0;

                    result.Add(new MicroPickItemDto(
                        line.Id,
                        stockCode,
                        stockName,
                        unit,
                        line.OrderedQty,
                        line.DeliveredQty,
                        isCompleted
                    ));
                }
            }
            
            return result.OrderBy(r => r.StockName).ToList();
        }
    }
}
