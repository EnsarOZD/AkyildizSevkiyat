using MediatR;

namespace Akyildiz.Sevkiyat.Application.Driver.Queries.GetActiveDriverSession
{
    public record GetActiveDriverSessionQuery : IRequest<ActiveDriverSessionDto?>;

    public record ActiveDriverSessionDto(
        Guid SessionId,
        int VehicleId,
        string PlateNumber,
        DateTime StartTime,
        double StartLatitude,
        double StartLongitude,
        int ElapsedMinutes
    );
}
