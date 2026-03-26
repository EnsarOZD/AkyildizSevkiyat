using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.AssignVehicle
{
    public record AssignVehicleCommand(int ShipmentId, string DriverName, string PlateNumber) : IRequest<AssignVehicleResult>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Dispatcher", "Warehouse" };
    }
}
