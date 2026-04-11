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
        int OpenWarningCount
    );

    public record DashboardProjectDto(
        int Id,
        int ProjectId,
        string ProjectCode,
        string ProjectName,
        bool IsMicroReady,
        DateTime? MicroReadyAt,
        bool IsAddedLater
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

            var shipmentZoneMap = await _context.Shipments
                .Where(s => s.ZonePreparationId != null && prepIds.Contains(s.ZonePreparationId.Value)
                         && s.Status != ShipmentStatus.Cancelled
                         && s.Status != ShipmentStatus.Passive)
                .Select(s => new { s.Id, ZonePreparationId = s.ZonePreparationId!.Value })
                .ToListAsync(cancellationToken);

            var shipmentIds = shipmentZoneMap.Select(s => s.Id).ToList();

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
                            p.IsAddedLater
                        )).ToList(),
                        errorsByZone.GetValueOrDefault(prep.Id, 0),
                        warnsByZone.GetValueOrDefault(prep.Id, 0)
                    ));
                }
            }

            return result;
        }
    }
}
