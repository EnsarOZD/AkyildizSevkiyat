namespace Akyildiz.Sevkiyat.WebApi.Controllers.Models
{
    public record BulkDispatchAsFreightRequest(
        List<int> ShipmentIds,
        string CarrierName,
        string? CarrierPlate = null,
        string? CarrierPhone = null
    );
}
