namespace Akyildiz.Sevkiyat.WebApi.Controllers.Models
{
    public record BulkDispatchAsCargoRequest(
        List<int> ShipmentIds,
        int CargoProvider,
        string? CargoTrackingNumber = null
    );
}
