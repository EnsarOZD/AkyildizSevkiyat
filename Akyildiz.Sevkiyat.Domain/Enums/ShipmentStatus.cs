namespace Akyildiz.Sevkiyat.Domain.Enums;

public enum ShipmentStatus
{
    Created = 0,
    AssignedToWarehouse = 1,
    Picking = 2,
    ReadyForDispatch = 3,
    AssignedToVehicle = 4,
    Dispatched = 5,           // Fiziksel yükleme onaylandı, araç yola çıktı

    // Final
    Delivered = 6,
    Cancelled = 7,
    ReturnedToWarehouse = 8,  // Tam iade — araçtan tamamen geri döndü
    Passive = 10
}
