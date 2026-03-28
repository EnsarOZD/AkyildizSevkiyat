namespace Akyildiz.Sevkiyat.Application.Common.Dtos
{
    public record ProjectDto(
        int Id,
        string Code,
        string Name,
        string? Region,
        bool IsActive,
        int? ZoneId,
        string? ZoneName,
        string? NetsisCariKodu,
        int? DeliveryOrder,
        double? Latitude,
        double? Longitude,
        string? Address
    );
}
