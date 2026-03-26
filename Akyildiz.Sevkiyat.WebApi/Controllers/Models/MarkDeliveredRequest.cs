namespace Akyildiz.Sevkiyat.WebApi.Controllers.Models
{
    public record MarkDeliveredRequest(
        string? DeliveryNote,
        string? DeliveryRecipient,
        string? DeliveryPhotoBase64,
        string? OverrideNote);
}
