using MediatR;

namespace Akyildiz.Sevkiyat.Application.Admin.Queries.GetActiveSessionsWithShipments
{
    public record GetActiveSessionsWithShipmentsQuery() : IRequest<List<ActiveSessionWithShipmentsDto>>;

    public record ActiveSessionWithShipmentsDto(
        Guid SessionId,
        int DriverId,
        string DriverFullName,
        int VehicleId,
        string PlateNumber,
        DateTime StartTime,
        int ElapsedMinutes,
        List<StuckShipmentDto> Shipments
    );

    public record StuckShipmentDto(
        int Id,
        string ProjectName,
        string? TalepNo,
        string? ExternalOrderNumber,
        string Status,
        int LineCount
    );
}
