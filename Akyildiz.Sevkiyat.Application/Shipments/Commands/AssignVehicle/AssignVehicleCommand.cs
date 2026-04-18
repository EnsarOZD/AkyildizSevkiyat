using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.AssignVehicle
{
    public record AssignVehicleCommand(int ShipmentId, int DriverId, int VehicleId) : IRequest<AssignVehicleResult>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting", "Driver", "Warehouse" };
    }
}
