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
            // Filter by Project Name + ZonePreparationId to catch fragmented projects
            // Using ToUpper + Trim for maximum robustness
            var projectNameUpper = (zpProject.Project?.Name ?? "").Trim().ToUpper();
            
            var lines = await _context.ShipmentLines
                .Include(sl => sl.Shipment)
                .Include(sl => sl.StockMaster)
                .Include(sl => sl.IssOrderLine) 
                .Where(sl => 
                    sl.Shipment.Project.Name.Trim().ToUpper() == projectNameUpper && 
                    sl.Shipment.ZonePreparationId == zpProject.ZonePreparationId && 
                    sl.Shipment.Status != ShipmentStatus.Cancelled &&
                    sl.Shipment.Status != ShipmentStatus.Passive &&
                    sl.Shipment.Status != ShipmentStatus.Created 
                )
                .Select(sl => new 
                {
                   sl.Id,
                   sl.OrderedQty,
                   sl.DeliveredQty,
                   sl.StockCode,
                   sl.StockName,
                   sl.Unit,
                   sl.StockMasterId,
                   PickingType = sl.StockMaster != null ? (PickingType?)sl.StockMaster.PickingType : null,
                   LocalStockCode = sl.StockMaster != null ? sl.StockMaster.StockCode : null,
                   LocalStockName = sl.StockMaster != null ? sl.StockMaster.StockName : null,
                   LocalUnit = sl.StockMaster != null ? sl.StockMaster.Unit.ToString() : null
                })
                .ToListAsync(cancellationToken);

            // 3. We map External Codes and Get Items
            // We NO LONGER GROUP by Stock. We list individual lines to allow editing each line individually.
            // This is safer for "Exchange Product" scenarios.
            
            var externalCodes = lines.Select(l => l.StockCode).Distinct().ToList();
            
            var mappings = await _context.StockMappings
                .Include(m => m.LocalStock)
                .Where(m => externalCodes.Select(x => x.ToLower()).Contains(m.ExternalStockCode.ToLower()))
                .ToListAsync(cancellationToken);

            var result = new List<MicroPickItemDto>();
            
            foreach(var line in lines)
            {
                bool isMicro = true;
                string stockCode = line.StockCode;
                string stockName = line.StockName;
                string unit = line.Unit.ToString() ?? "ADET";

                // Prioritize direct link to StockMaster if it exists
                if (line.StockMasterId.HasValue && line.PickingType.HasValue)
                {
                    if (line.PickingType == PickingType.Macro) isMicro = false;
                    stockCode = line.LocalStockCode ?? line.StockCode;
                    stockName = line.LocalStockName ?? line.StockName;
                    unit = line.LocalUnit ?? unit;
                }
                else 
                {
                    // Fallback to mappings
                    var mapping = mappings.FirstOrDefault(m => m.ExternalStockCode.Equals(line.StockCode, System.StringComparison.OrdinalIgnoreCase));
                    if (mapping != null && mapping.LocalStock != null)
                    {
                        if (mapping.LocalStock.PickingType == PickingType.Macro) isMicro = false;
                        stockCode = mapping.LocalStock.StockCode;
                        stockName = mapping.LocalStock.StockName;
                        unit = mapping.LocalStock.Unit.ToString();
                    }
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
