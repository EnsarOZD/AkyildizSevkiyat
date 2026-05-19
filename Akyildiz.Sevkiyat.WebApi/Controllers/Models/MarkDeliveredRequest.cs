namespace Akyildiz.Sevkiyat.WebApi.Controllers.Models
{
    public record MarkDeliveredRequest(
        string? DeliveryNote,
        string? DeliveryRecipient,
        List<string>? DeliveryPhotosBase64,
        string? OverrideNote,
        double? DeliveryLatitude = null,
        double? DeliveryLongitude = null);
}
