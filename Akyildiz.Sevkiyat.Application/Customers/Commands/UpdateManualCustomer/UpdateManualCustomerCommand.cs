using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.Customers.Commands.UpdateManualCustomer
{
    public record UpdateManualCustomerCommand(
        int Id,
        string Name,
        string NetsisCariKodu,
        string? NetsisTeslimCariKodu,
        OperationType OperationType,
        string? Address,
        string? CityName,
        string? DistrictName,
        double? Latitude,
        double? Longitude,
        string? DefaultContactName,
        string? DefaultContactPhone
    ) : IRequest, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager", "Accounting" };
    }
}
