using MediatR;

namespace Akyildiz.Sevkiyat.Application.Driver.Commands.StartDriverSession
{
    public record StartDriverSessionCommand(
        string QrCode,
        double Latitude,
        double Longitude,
        string? DeviceFingerprint = null,
        string? StartOdometerPhotoBase64 = null,
        int? StartOdometerKm = null,
        // Sefere dahil sevkiyatları çözmek için okutulan irsaliye numarası (zorunlu)
        string? IrsaliyeNo = null
    ) : IRequest<StartDriverSessionResult>;

    public record StartDriverSessionResult(
        Guid SessionId,
        string VehiclePlateNumber,
        DateTime StartTime,
        int ShipmentCount
    );
}
