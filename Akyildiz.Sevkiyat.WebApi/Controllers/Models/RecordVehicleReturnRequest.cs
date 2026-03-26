using Akyildiz.Sevkiyat.Domain.Enums;

namespace Akyildiz.Sevkiyat.WebApi.Controllers.Models
{
    public record RecordVehicleReturnRequest(
        List<ReturnLineRequest> Lines,
        string? ReturnNote = null,
        string? OverrideNote = null
    );

    public record ReturnLineRequest(
        int ShipmentLineId,
        decimal ReturnedQty,
        ReturnReason ReturnReason
    );
}
