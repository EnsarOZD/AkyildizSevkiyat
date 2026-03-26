namespace Akyildiz.Sevkiyat.WebApi.Controllers.Models
{
    public sealed record UpdateDeliveredQtyRequest(
        decimal DeliveredQty,
        string? DifferenceReason,
        string? Note
    );
}