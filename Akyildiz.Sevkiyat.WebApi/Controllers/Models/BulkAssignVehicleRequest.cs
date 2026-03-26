namespace Akyildiz.Sevkiyat.WebApi.Controllers.Models
{
    public record BulkAssignVehicleRequest(
        List<int> ShipmentIds,
        string DriverName,
        string PlateNumber
    );
}
