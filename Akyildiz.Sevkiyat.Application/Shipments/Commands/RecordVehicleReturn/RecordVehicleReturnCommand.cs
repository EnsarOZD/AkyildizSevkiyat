using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.RecordVehicleReturn
{
    /// <summary>
    /// Araç depodan döndüğünde hangi kalemlerin kaç adet geri geldiğini kaydeder.
    /// Her satır için ReturnedQty ve ReturnReason set edilir.
    /// Tüm kalemlerin miktarı tam iade ise sevkiyat ReturnedToWarehouse durumuna geçer;
    /// kısmi iade ise Delivered durumunda kalır.
    /// </summary>
    public record RecordVehicleReturnCommand(
        int ShipmentId,
        List<ReturnLineDto> Lines,
        string? ReturnNote = null,
        string? OverrideNote = null
    ) : IRequest<Unit>;

    public record ReturnLineDto(
        int ShipmentLineId,
        decimal ReturnedQty,
        ReturnReason ReturnReason
    );
}
