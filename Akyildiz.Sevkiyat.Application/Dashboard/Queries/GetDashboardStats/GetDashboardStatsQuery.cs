using MediatR;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Dashboard.Queries.GetDashboardStats
{
    public record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;

    public class DashboardStatsDto
    {
        // ── Shipment KPIs (shared) ────────────────────────────────────────────
        public int TotalActiveShipments { get; set; }
        public int ShipmentsToday { get; set; }
        public int ShipmentsOverdue { get; set; }
        public int ShipmentsDeliveredThisWeek { get; set; }

        // Status breakdown (active only)
        public int StatusDraft { get; set; }
        public int StatusWarehouse { get; set; }
        public int StatusPicking { get; set; }
        public int StatusReady { get; set; }
        public int StatusOnRoute { get; set; }

        // Floating returns
        public int PendingFloatingReturns { get; set; }

        // Stock alerts
        public int CriticalStockCount { get; set; }

        // Procurement alerts
        public int PendingGoodsReceiptsCount { get; set; }
        public int PendingPOApprovalCount { get; set; }

        // Today's shipments not yet ready (Warehouse / Manager interest)
        public int TodayShipmentsNotReadyCount { get; set; }

        // Recent shipments (last 5 active, ordered by delivery date)
        public List<RecentShipmentDto> RecentShipments { get; set; } = new();

        // ── Manager extras ────────────────────────────────────────────────────
        /// <summary>Bugün araç ataması bekleyen sevkiyatlar (Status = ReadyForDispatch, today).</summary>
        public List<RecentShipmentDto> WaitingForVehicleToday { get; set; } = new();
        public int WaitingForVehicleTodayCount { get; set; }

        // ── Accounting extras ─────────────────────────────────────────────────
        public int NewIssOrdersTodayCount { get; set; }
        public int PendingStockMappingCount { get; set; }
        public int PendingNetsisTransferCount { get; set; }
        public int TodayDispatchedCount { get; set; }
        public int MissingItemsMailPendingCount { get; set; }
        public List<RecentShipmentDto> PendingNetsisShipments { get; set; } = new();
        public List<RecentShipmentDto> MissingItemsPendingShipments { get; set; } = new();
        public ImportBatchSummaryDto? LastIssImportBatch { get; set; }

        // ── Warehouse extras ──────────────────────────────────────────────────
        public List<ActiveZonePreparationDto> ActiveZonePreparations { get; set; } = new();
        public List<RecentShipmentDto> TodayPreparationNeededShipments { get; set; } = new();
    }

    public class RecentShipmentDto
    {
        public int Id { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string TalepNo { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime DeliveryDate { get; set; }
        public string? IrsaliyeNo { get; set; }
    }

    public class ImportBatchSummaryDto
    {
        public int Id { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public int NewCount { get; set; }
        public int NeedsMappingCount { get; set; }
        public int FailedCount { get; set; }
    }

    public class ActiveZonePreparationDto
    {
        public int Id { get; set; }
        public string ZoneName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime DeliveryDate { get; set; }
        public int ShipmentCount { get; set; }
        public string? DriverName { get; set; }
        public string? PlateNumber { get; set; }
    }

    public class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetDashboardStatsQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
        {
            var today = DateTime.Today;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var role = _currentUserService.Role;

            // Role flags — which sections to populate
            var isManagerOrAdmin = role == UserRole.Manager || role == UserRole.Admin;
            var isAccounting = role == UserRole.Accounting;
            var isWarehouse = role == UserRole.Warehouse;

            var activeStatuses = new[]
            {
                ShipmentStatus.Created,
                ShipmentStatus.AssignedToWarehouse,
                ShipmentStatus.Picking,
                ShipmentStatus.ReadyForDispatch,
                ShipmentStatus.AssignedToVehicle,
            };

            // ── Shared aggregates ─────────────────────────────────────────────
            var activeShipments = await _context.Shipments
                .Where(s => activeStatuses.Contains(s.Status))
                .Select(s => new { s.Status, s.DeliveryDate })
                .ToListAsync(cancellationToken);

            var deliveredThisWeek = await _context.Shipments
                .CountAsync(s => s.Status == ShipmentStatus.Delivered
                    && s.DeliveredAt != null
                    && s.DeliveredAt >= weekStart,
                    cancellationToken);

            var pendingFloatingReturns = await _context.FloatingReturns
                .CountAsync(fr => fr.Status == FloatingReturnStatus.Pending, cancellationToken);

            var criticalStockCount = await _context.StockMasters
                .CountAsync(s => s.IsActive && s.MinStockQty.HasValue
                    && (s.OnHandQty - s.ReservedQty) < s.MinStockQty.Value, cancellationToken);

            var pendingGoodsReceiptsCount = await _context.GoodsReceipts
                .CountAsync(gr => gr.Status == GoodsReceiptStatus.Draft, cancellationToken);

            var pendingPOApprovalCount = await _context.PurchaseOrders
                .CountAsync(po => po.Status == PurchaseOrderStatus.Draft, cancellationToken);

            var notReadyStatuses = new[]
            {
                ShipmentStatus.Created,
                ShipmentStatus.AssignedToWarehouse,
                ShipmentStatus.Picking,
            };
            var todayShipmentsNotReadyCount = await _context.Shipments
                .CountAsync(s => s.DeliveryDate.Date == today
                    && notReadyStatuses.Contains(s.Status), cancellationToken);

            var recentShipments = await _context.Shipments
                .Include(s => s.Project)
                .Include(s => s.IssOrder)
                .Where(s => activeStatuses.Contains(s.Status))
                .OrderBy(s => s.DeliveryDate)
                .Take(5)
                .Select(s => new RecentShipmentDto
                {
                    Id = s.Id,
                    ProjectName = s.Project.Name,
                    TalepNo = s.IssOrder != null
                        ? (s.IssOrder.TalepNo ?? s.IssOrder.ExternalOrderNumber ?? "")
                        : (s.TalepNo ?? ""),
                    Status = s.Status.ToString(),
                    DeliveryDate = s.DeliveryDate,
                    IrsaliyeNo = s.IrsaliyeNo,
                })
                .ToListAsync(cancellationToken);

            var dto = new DashboardStatsDto
            {
                TotalActiveShipments = activeShipments.Count,
                ShipmentsToday = activeShipments.Count(s => s.DeliveryDate.Date == today),
                ShipmentsOverdue = activeShipments.Count(s => s.DeliveryDate.Date < today),
                ShipmentsDeliveredThisWeek = deliveredThisWeek,

                StatusDraft = activeShipments.Count(s => s.Status == ShipmentStatus.Created),
                StatusWarehouse = activeShipments.Count(s => s.Status == ShipmentStatus.AssignedToWarehouse),
                StatusPicking = activeShipments.Count(s => s.Status == ShipmentStatus.Picking),
                StatusReady = activeShipments.Count(s => s.Status == ShipmentStatus.ReadyForDispatch),
                StatusOnRoute = activeShipments.Count(s => s.Status == ShipmentStatus.AssignedToVehicle),

                PendingFloatingReturns = pendingFloatingReturns,
                CriticalStockCount = criticalStockCount,
                PendingGoodsReceiptsCount = pendingGoodsReceiptsCount,
                PendingPOApprovalCount = pendingPOApprovalCount,
                TodayShipmentsNotReadyCount = todayShipmentsNotReadyCount,
                RecentShipments = recentShipments,
            };

            // ── Manager extras ────────────────────────────────────────────────
            if (isManagerOrAdmin)
            {
                var waitingForVehicleQuery = _context.Shipments
                    .Include(s => s.Project)
                    .Include(s => s.IssOrder)
                    .Where(s => s.Status == ShipmentStatus.ReadyForDispatch
                                && s.DeliveryDate.Date == today
                                && s.AssignedDriverId == null);

                dto.WaitingForVehicleTodayCount = await waitingForVehicleQuery.CountAsync(cancellationToken);
                dto.WaitingForVehicleToday = await waitingForVehicleQuery
                    .OrderBy(s => s.DeliveryDate)
                    .Take(5)
                    .Select(s => new RecentShipmentDto
                    {
                        Id = s.Id,
                        ProjectName = s.Project.Name,
                        TalepNo = s.IssOrder != null
                            ? (s.IssOrder.TalepNo ?? s.IssOrder.ExternalOrderNumber ?? "")
                            : (s.TalepNo ?? ""),
                        Status = s.Status.ToString(),
                        DeliveryDate = s.DeliveryDate,
                        IrsaliyeNo = s.IrsaliyeNo,
                    })
                    .ToListAsync(cancellationToken);
            }

            // ── Accounting extras ─────────────────────────────────────────────
            if (isAccounting || isManagerOrAdmin)
            {
                dto.NewIssOrdersTodayCount = await _context.IssOrders
                    .CountAsync(o => o.OrderDate >= today, cancellationToken);

                dto.PendingStockMappingCount = await _context.StockMappings
                    .CountAsync(m => m.MatchStatus == MatchStatus.Unmapped, cancellationToken);

                // Shipments that have left the warehouse but aren't Netsis-transferred yet
                var netsisPendingStatuses = new[]
                {
                    ShipmentStatus.ReadyForDispatch,
                    ShipmentStatus.AssignedToVehicle,
                    ShipmentStatus.Dispatched,
                    ShipmentStatus.Delivered,
                };
                var netsisPendingQuery = _context.Shipments
                    .Include(s => s.Project)
                    .Include(s => s.IssOrder)
                    .Where(s => netsisPendingStatuses.Contains(s.Status)
                                && s.NetsisTransferredAt == null);

                dto.PendingNetsisTransferCount = await netsisPendingQuery.CountAsync(cancellationToken);
                dto.PendingNetsisShipments = await netsisPendingQuery
                    .OrderByDescending(s => s.DeliveryDate)
                    .Take(5)
                    .Select(s => new RecentShipmentDto
                    {
                        Id = s.Id,
                        ProjectName = s.Project.Name,
                        TalepNo = s.IssOrder != null
                            ? (s.IssOrder.TalepNo ?? s.IssOrder.ExternalOrderNumber ?? "")
                            : (s.TalepNo ?? ""),
                        Status = s.Status.ToString(),
                        DeliveryDate = s.DeliveryDate,
                        IrsaliyeNo = s.IrsaliyeNo,
                    })
                    .ToListAsync(cancellationToken);

                dto.TodayDispatchedCount = await _context.Shipments
                    .CountAsync(s => s.Status == ShipmentStatus.Dispatched
                        && s.DispatchedAt != null
                        && s.DispatchedAt.Value.Date == today, cancellationToken);

                // Missing-items: delivered shipments with a line shortage and no notification mail
                var missingItemsQuery = _context.Shipments
                    .Include(s => s.Project)
                    .Include(s => s.IssOrder)
                    .Include(s => s.Lines)
                    .Where(s => s.Status == ShipmentStatus.Delivered
                                && s.MissingItemsMailSentAt == null
                                && s.Lines.Any(l => l.DeliveredQty < l.OrderedQty));

                dto.MissingItemsMailPendingCount = await missingItemsQuery.CountAsync(cancellationToken);
                dto.MissingItemsPendingShipments = await missingItemsQuery
                    .OrderByDescending(s => s.DeliveredAt)
                    .Take(5)
                    .Select(s => new RecentShipmentDto
                    {
                        Id = s.Id,
                        ProjectName = s.Project.Name,
                        TalepNo = s.IssOrder != null
                            ? (s.IssOrder.TalepNo ?? s.IssOrder.ExternalOrderNumber ?? "")
                            : (s.TalepNo ?? ""),
                        Status = s.Status.ToString(),
                        DeliveryDate = s.DeliveryDate,
                        IrsaliyeNo = s.IrsaliyeNo,
                    })
                    .ToListAsync(cancellationToken);

                dto.LastIssImportBatch = await _context.ImportBatches
                    .OrderByDescending(b => b.StartedAt)
                    .Take(1)
                    .Select(b => new ImportBatchSummaryDto
                    {
                        Id = b.Id,
                        StartedAt = b.StartedAt,
                        CompletedAt = b.CompletedAt,
                        Status = b.Status.ToString(),
                        NewCount = b.NewCount,
                        NeedsMappingCount = b.NeedsMappingCount,
                        FailedCount = b.FailedCount,
                    })
                    .FirstOrDefaultAsync(cancellationToken);
            }

            // ── Warehouse extras ──────────────────────────────────────────────
            if (isWarehouse || isManagerOrAdmin)
            {
                var activeZpStatuses = new[]
                {
                    ZonePreparationStatus.MicroPicking,
                    ZonePreparationStatus.MicroReady,
                    ZonePreparationStatus.MacroPicking,
                    ZonePreparationStatus.GidaHazirlik,
                    ZonePreparationStatus.ReadyForDriverInfo,
                    ZonePreparationStatus.ReadyForTransfer,
                };

                dto.ActiveZonePreparations = await _context.ZonePreparations
                    .Include(zp => zp.Zone)
                    .Include(zp => zp.Driver)
                    .Include(zp => zp.Vehicle)
                    .Where(zp => activeZpStatuses.Contains(zp.Status))
                    .OrderBy(zp => zp.DeliveryDate)
                    .ThenBy(zp => zp.Zone.Order)
                    .Take(8)
                    .Select(zp => new ActiveZonePreparationDto
                    {
                        Id = zp.Id,
                        ZoneName = zp.Zone.Name,
                        Status = zp.Status.ToString(),
                        DeliveryDate = zp.DeliveryDate,
                        ShipmentCount = _context.Shipments.Count(s => s.ZonePreparationId == zp.Id),
                        DriverName = zp.Driver != null ? zp.Driver.FullName : null,
                        PlateNumber = zp.Vehicle != null ? zp.Vehicle.PlateNumber : null,
                    })
                    .ToListAsync(cancellationToken);

                dto.TodayPreparationNeededShipments = await _context.Shipments
                    .Include(s => s.Project)
                    .Include(s => s.IssOrder)
                    .Where(s => s.DeliveryDate.Date == today
                                && notReadyStatuses.Contains(s.Status))
                    .OrderBy(s => s.Status)
                    .ThenBy(s => s.DeliveryDate)
                    .Take(5)
                    .Select(s => new RecentShipmentDto
                    {
                        Id = s.Id,
                        ProjectName = s.Project.Name,
                        TalepNo = s.IssOrder != null
                            ? (s.IssOrder.TalepNo ?? s.IssOrder.ExternalOrderNumber ?? "")
                            : (s.TalepNo ?? ""),
                        Status = s.Status.ToString(),
                        DeliveryDate = s.DeliveryDate,
                        IrsaliyeNo = s.IrsaliyeNo,
                    })
                    .ToListAsync(cancellationToken);
            }

            return dto;
        }
    }
}
