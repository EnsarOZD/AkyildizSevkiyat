using MediatR;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Dashboard.Queries.GetDashboardStats
{
    public record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;

    public class DashboardStatsDto
    {
        // Shipment KPIs
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

        // Today's shipments not yet ready
        public int TodayShipmentsNotReadyCount { get; set; }

        // Recent shipments (last 5 active, ordered by delivery date)
        public List<RecentShipmentDto> RecentShipments { get; set; } = new();
    }

    public class RecentShipmentDto
    {
        public int Id { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string TalepNo { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime DeliveryDate { get; set; }
    }

    public class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
    {
        private readonly IApplicationDbContext _context;

        public GetDashboardStatsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
        {
            var today = DateTime.Today;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);

            var activeStatuses = new[]
            {
                ShipmentStatus.Created,
                ShipmentStatus.AssignedToWarehouse,
                ShipmentStatus.Picking,
                ShipmentStatus.ReadyForDispatch,
                ShipmentStatus.AssignedToVehicle,
            };

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

            var todayDate = DateTime.Today;
            var notReadyStatuses = new[]
            {
                ShipmentStatus.Created,
                ShipmentStatus.AssignedToWarehouse,
                ShipmentStatus.Picking,
            };
            var todayShipmentsNotReadyCount = await _context.Shipments
                .CountAsync(s => s.DeliveryDate.Date == todayDate
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
                    TalepNo = s.IssOrder.TalepNo ?? s.IssOrder.ExternalOrderNumber ?? "",
                    Status = s.Status.ToString(),
                    DeliveryDate = s.DeliveryDate,
                })
                .ToListAsync(cancellationToken);

            return new DashboardStatsDto
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
        }
    }
}
