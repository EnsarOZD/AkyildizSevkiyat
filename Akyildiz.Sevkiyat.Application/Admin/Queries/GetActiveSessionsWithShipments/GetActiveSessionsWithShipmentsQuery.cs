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
        List<StuckShipmentDto> Shipments,
        // Özet + ilerleme haritası (#18) — teslim edilenler dahil tüm sefer durakları
        int TotalProjects,
        int DeliveredProjects,
        List<ActiveSessionStopDto> Stops
    );

    public record StuckShipmentDto(
        int Id,
        int ProjectId,
        string ProjectName,
        string? TalepNo,
        string? ExternalOrderNumber,
        string Status,
        int LineCount
    );

    /// <summary>İlerleme haritası için proje bazında durak (teslim edildi/bekliyor).</summary>
    public record ActiveSessionStopDto(
        int ProjectId,
        string ProjectName,
        string? ProjectAddress,
        double? Latitude,
        double? Longitude,
        bool IsDelivered
    );
}
