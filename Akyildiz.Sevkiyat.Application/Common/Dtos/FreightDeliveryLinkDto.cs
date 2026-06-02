namespace Akyildiz.Sevkiyat.Application.Common.Dtos
{
    /// <summary>
    /// Nakliye gönderiminde proje başına oluşturulan teslim linki bilgisi.
    /// Frontend bunu kullanarak `{origin}/teslim/{token}` linkini ve wa.me mesajını kurar.
    /// </summary>
    public record FreightDeliveryLinkDto(
        int ProjectId,
        string ProjectName,
        string Token,
        string? CarrierPhone,
        int ShipmentCount
    );
}
