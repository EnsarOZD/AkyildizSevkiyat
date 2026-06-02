using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.CreateManualShipment
{
    public record CreateManualShipmentLineInput(
        int StockMasterId,
        decimal Qty
    );

    public record CreateManualShipmentCommand(
        int CustomerId,
        DateTime DeliveryDate,
        bool RequiresWarehousePreparation,
        IReadOnlyList<CreateManualShipmentLineInput> Lines,
        string? Notes = null
    ) : IRequest<int>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager", "Accounting" };
    }
}
