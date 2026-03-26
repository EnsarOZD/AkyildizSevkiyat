using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.UpdateShipmentDetails
{
    public record ShipmentLineUpdateDto(int? LineId, string StockCode, string StockName, decimal OrderedQty, StockUnit Unit);
    public record UpdateShipmentDetailsCommand(int ShipmentId, DateTime DeliveryDate, List<ShipmentLineUpdateDto> Lines) : IRequest<Unit>;
}
