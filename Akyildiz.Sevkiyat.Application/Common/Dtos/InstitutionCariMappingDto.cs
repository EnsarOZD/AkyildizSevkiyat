namespace Akyildiz.Sevkiyat.Application.Common.Dtos
{
    public record InstitutionCariMappingDto(
        int Id,
        string InstitutionCode,
        string NetsisCariKodu,
        string? Description,
        bool IsActive
    );
}
