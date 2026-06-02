using Akyildiz.Sevkiyat.Domain.Enums;

namespace Akyildiz.Sevkiyat.Application.Common.Dtos
{
    /// <summary>
    /// Manuel müşteri kartı için liste/detay DTO'su. Project entity'sinden Source=Manual
    /// kayıtlarını temsil eder.
    /// </summary>
    public record CustomerDto(
        int Id,
        string Code,
        string Name,
        bool IsActive,
        OperationType OperationType,
        string? NetsisCariKodu,
        string? NetsisTeslimCariKodu,
        string? Address,
        string? CityName,
        string? DistrictName,
        double? Latitude,
        double? Longitude,
        string? DefaultContactName,
        string? DefaultContactPhone
    );
}
