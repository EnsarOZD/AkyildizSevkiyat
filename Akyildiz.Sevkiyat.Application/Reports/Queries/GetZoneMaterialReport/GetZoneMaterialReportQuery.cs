using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Reports.Queries.GetZoneMaterialReport
{
    public enum QtyMode
    {
        Ordered,
        Delivered
    }

    public record GetZoneMaterialReportQuery(
        DateTime DeliveryDate,
        int? ZoneId = null,
        bool IncludeDelivered = false,
        QtyMode QtyMode = QtyMode.Ordered) : IRequest<List<ZoneMaterialRowDto>>;

    public class GetZoneMaterialReportQueryHandler : IRequestHandler<GetZoneMaterialReportQuery, List<ZoneMaterialRowDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetZoneMaterialReportQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ZoneMaterialRowDto>> Handle(GetZoneMaterialReportQuery request, CancellationToken cancellationToken)
        {
            var date = request.DeliveryDate.Date;

            // 1. Base Filter for Shipments
            var shipmentQuery = _context.Shipments
                .Include(s => s.ZonePreparation!)
                    .ThenInclude(zp => zp.Zone)
                .Include(s => s.Project)
                    .ThenInclude(p => p.Zone)
                .Where(s => s.DeliveryDate.Date == date);

            if (request.IncludeDelivered)
            {
                shipmentQuery = shipmentQuery.Where(s => s.Status == ShipmentStatus.AssignedToVehicle || s.Status == ShipmentStatus.Delivered);
            }
            else
            {
                shipmentQuery = shipmentQuery.Where(s => s.Status == ShipmentStatus.AssignedToVehicle);
            }

            // 2. Further filter by Zone if requested
            // Note: Using a single source of truth for the zone as per plan. 
            // Operational Zone = ZonePreparation.ZoneId ?? Project.ZoneId
            
            var shipments = await shipmentQuery.ToListAsync(cancellationToken);

            // 3. Process grouping in memory for better control over standardization/fallbacks
            // Fetch all lines for these shipments
            var shipmentIds = shipments.Select(s => s.Id).ToList();
            var lines = await _context.ShipmentLines
                .Where(l => shipmentIds.Contains(l.ShipmentId))
                .ToListAsync(cancellationToken);

            // Fetch StockMasters for standardization
            var stockCodes = lines.Select(l => l.StockCode).Distinct().ToList();
            var stockMasters = await _context.StockMasters
                .Where(s => stockCodes.Contains(s.StockCode))
                .ToDictionaryAsync(s => s.StockCode, s => s, cancellationToken);

            var reportRows = lines
                .GroupBy(l => {
                    var shipment = shipments.First(s => s.Id == l.ShipmentId);
                    // Single Source of Truth for Zone
                    int zoneId = shipment.ZonePreparation?.ZoneId ?? shipment.Project?.ZoneId ?? 0;
                    return new { ZoneId = zoneId, l.StockCode };
                })
                .Select(g => {
                    var firstLine = g.First();
                    var shipment = shipments.First(s => s.Id == firstLine.ShipmentId);
                    
                    int zoneId = g.Key.ZoneId;
                    string zoneName = shipment.ZonePreparation?.Zone?.Name ?? shipment.Project?.Zone?.Name ?? "Unassigned";

                    if (request.ZoneId.HasValue && zoneId != request.ZoneId.Value) return null;

                    stockMasters.TryGetValue(g.Key.StockCode, out var master);

                    return new ZoneMaterialRowDto
                    {
                        ZoneId = zoneId,
                        ZoneName = zoneName,
                        StockMasterId = master?.Id,
                        StockCode = master?.StockCode ?? firstLine.StockCode,
                        StockName = master?.StockName ?? firstLine.StockName,
                        Unit = master?.Unit.ToString() ?? firstLine.Unit.ToString(),
                        TotalQty = g.Sum(l => request.QtyMode == QtyMode.Ordered ? l.OrderedQty : l.DeliveredQty),
                        ShipmentCount = g.Select(l => l.ShipmentId).Distinct().Count()
                    };
                })
                .Where(r => r != null)
                .OrderBy(r => r!.ZoneName)
                .ThenBy(r => r!.StockCode)
                .ToList();

            return reportRows!;
        }
    }
}
