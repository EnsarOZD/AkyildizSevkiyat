using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Events;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.Shipments.Notifications;

public class ShipmentNotificationHandler : INotificationHandler<ShipmentStatusChangedEvent>
{
    private readonly INotificationService _notifications;

    public ShipmentNotificationHandler(INotificationService notifications)
    {
        _notifications = notifications;
    }

    public async Task Handle(ShipmentStatusChangedEvent ev, CancellationToken ct)
    {
        switch (ev.NewStatus)
        {
            case ShipmentStatus.AssignedToWarehouse:
                await _notifications.SendToRolesAsync(
                    [UserRole.Warehouse],
                    "Yeni Sevkiyat Atandı",
                    $"Sevkiyat #{ev.ShipmentId} depoya atandı, toplamaya hazır.",
                    $"/shipments/{ev.ShipmentId}",
                    "shipment_warehouse_assigned",
                    ct);
                break;

            case ShipmentStatus.Dispatched:
                await _notifications.SendToRolesAsync(
                    [UserRole.Driver],
                    "Sevkiyat Yolda",
                    $"Sevkiyat #{ev.ShipmentId} yola çıktı. Teslimat listeni kontrol et.",
                    "/driver",
                    "shipment_dispatched",
                    ct);
                break;

            case ShipmentStatus.Delivered:
                await _notifications.SendToRolesAsync(
                    [UserRole.Admin, UserRole.Manager],
                    "Teslim Edildi",
                    $"Sevkiyat #{ev.ShipmentId} başarıyla teslim edildi.",
                    $"/shipments/{ev.ShipmentId}",
                    "shipment_delivered",
                    ct);
                break;
        }
    }
}
