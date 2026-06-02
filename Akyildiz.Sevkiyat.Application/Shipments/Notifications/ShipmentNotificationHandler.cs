using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Shipments.Notifications;

public class ShipmentNotificationHandler : INotificationHandler<ShipmentStatusChangedEvent>
{
    private readonly INotificationService _notifications;
    private readonly IApplicationDbContext _context;

    public ShipmentNotificationHandler(INotificationService notifications, IApplicationDbContext context)
    {
        _notifications = notifications;
        _context = context;
    }

    public async Task Handle(ShipmentStatusChangedEvent ev, CancellationToken ct)
    {
        // Bildirim metninde sevkiyat numarasının yanında proje adını göster
        var projectName = await _context.Shipments
            .Where(s => s.Id == ev.ShipmentId)
            .Select(s => s.Project.Name)
            .FirstOrDefaultAsync(ct);

        var label = string.IsNullOrWhiteSpace(projectName)
            ? $"Sevkiyat #{ev.ShipmentId}"
            : $"Sevkiyat #{ev.ShipmentId} — {projectName}";

        switch (ev.NewStatus)
        {
            case ShipmentStatus.AssignedToWarehouse:
                await _notifications.SendToRolesAsync(
                    [UserRole.Warehouse],
                    "Yeni Sevkiyat Atandı",
                    $"{label} depoya atandı, toplamaya hazır.",
                    $"/shipments/{ev.ShipmentId}",
                    "shipment_warehouse_assigned",
                    ct);
                break;

            case ShipmentStatus.Dispatched:
                await _notifications.SendToRolesAsync(
                    [UserRole.Driver],
                    "Sevkiyat Yolda",
                    $"{label} yola çıktı. Teslimat listeni kontrol et.",
                    "/driver",
                    "shipment_dispatched",
                    ct);
                break;

            case ShipmentStatus.Delivered:
                await _notifications.SendToRolesAsync(
                    [UserRole.Admin, UserRole.Manager],
                    "Teslim Edildi",
                    $"{label} başarıyla teslim edildi.",
                    $"/shipments/{ev.ShipmentId}",
                    "shipment_delivered",
                    ct);
                break;
        }
    }
}
