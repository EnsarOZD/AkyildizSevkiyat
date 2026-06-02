using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.Customers.Commands.CreateManualCustomer
{
    public record CreateManualCustomerCommand(
        string Name,
        string NetsisCariKodu,
        string? NetsisTeslimCariKodu = null,
        OperationType OperationType = OperationType.Catering,
        string? Address = null,
        string? CityName = null,
        string? DistrictName = null,
        double? Latitude = null,
        double? Longitude = null,
        string? DefaultContactName = null,
        string? DefaultContactPhone = null
    ) : IRequest<int>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager", "Accounting" };
    }
}
