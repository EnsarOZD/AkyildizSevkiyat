using MediatR;

namespace Akyildiz.Sevkiyat.Application.Driver.Commands.EndDriverSession
{
    public record EndDriverSessionCommand(
        string QrCode,
        double Latitude,
        double Longitude
    ) : IRequest<EndDriverSessionResult>;

    public record EndDriverSessionResult(
        Guid SessionId,
        int TotalDurationMinutes
    );
}
