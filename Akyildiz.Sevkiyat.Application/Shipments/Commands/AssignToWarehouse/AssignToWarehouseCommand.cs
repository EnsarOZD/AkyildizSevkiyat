using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.AssignToWarehouse
{
    public record AssignToWarehouseCommand(int ShipmentId) : IRequest<AssignToWarehouseResult>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Warehouse" };
    }

    public class AssignToWarehouseResult
    {
        public List<string> Warnings { get; set; } = new();
    }
}
