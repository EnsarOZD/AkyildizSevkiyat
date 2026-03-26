namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.AssignVehicle
{
    public record DriverWarning(int ActiveShipmentCount, string Message);

    public record AssignVehicleResult(int ShipmentId, DriverWarning? Warning);
}
