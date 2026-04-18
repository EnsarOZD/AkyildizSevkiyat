using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.ToggleShipmentStatus
{
    public record ToggleShipmentStatusCommand(int ShipmentId, bool SetPassive, string? Reason = null) : IRequest<Unit>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting" };
    }
}
