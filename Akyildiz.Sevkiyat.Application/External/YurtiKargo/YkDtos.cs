namespace Akyildiz.Sevkiyat.Application.External.YurtiKargo
{
    public record YkCreateShipmentRequest(
        string CargoKey,
        string InvoiceKey,
        string ReceiverName,
        string ReceiverAddress,
        string ReceiverPhone,
        string? ReceiverPhone2 = null,
        string? ReceiverCityName = null,
        string? ReceiverTownName = null,
        int PieceCount = 1,
        decimal Desi = 1m,
        decimal Kg = 1m
    );

    public record YkCreateShipmentResult(
        bool IsSuccess,
        string? ErrorMessage,

        // Yurtiçi operation-level fields
        string? OperationStatus,
        string? OperationMessage,
        string? ErrCode,
        string? ErrMessage,

        // Tracking
        int? JobId,
        string? DocId,
        string? InvoiceKey,
        string? Barcode
    );

    public record YkShipmentStatus(
        string CargoKey,
        string? StatusCode,
        string? StatusDescription,
        string? Barcode,
        DateTime? LastUpdate
    );
}
