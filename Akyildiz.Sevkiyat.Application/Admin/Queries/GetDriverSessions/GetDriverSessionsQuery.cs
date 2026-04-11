using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.Admin.Queries.GetDriverSessions
{
    public record GetDriverSessionsQuery(
        DateTime FromDate,
        DateTime ToDate,
        int? DriverId,
        int? VehicleId,
        DriverSessionStatus? Status,
        int PageNumber = 1,
        int PageSize = 50
    ) : IRequest<GetDriverSessionsResult>;

    public record GetDriverSessionsResult(
        List<DriverSessionDto> Items,
        int TotalCount,
        int PageNumber,
        int PageSize
    );

    public record DriverSessionDto(
        Guid Id,
        int DriverId,
        string DriverFullName,
        int VehicleId,
        string PlateNumber,
        DateTime StartTime,
        DateTime? EndTime,
        int? TotalDurationMinutes,
        double StartLatitude,
        double StartLongitude,
        double? EndLatitude,
        double? EndLongitude,
        DriverSessionStatus Status,
        string? Notes
    );
}
