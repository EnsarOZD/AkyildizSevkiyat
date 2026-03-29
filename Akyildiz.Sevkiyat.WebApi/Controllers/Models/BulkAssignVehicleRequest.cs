namespace Akyildiz.Sevkiyat.WebApi.Controllers.Models
{
    public record BulkAssignVehicleRequest(
        List<int> ShipmentIds,
        int DriverId,
        int VehicleId
    );
}
