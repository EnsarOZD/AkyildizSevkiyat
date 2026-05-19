using Akyildiz.Sevkiyat.Domain.Enums;

namespace Akyildiz.Sevkiyat.Application.RouteOptimization.Dtos
{
    public record ProjectSyncComparisonDto(
        string ProjectCode,
        string ProjectName,
        string? CurrentName,
        string? IssName,
        bool NameChanged,
        string? CurrentAddress,
        string? IssAddress,
        bool AddressChanged
    )
    {
        public bool HasDifference => NameChanged || AddressChanged;
    }

    public record SyncApprovalRequestDto(
        string ProjectCode,
        bool ApproveNameUpdate,
        bool ApproveAddressUpdate
    );

    public record RouteOptimizationRequestDto(
        List<string> ProjectCodes,
        string? StartAddress,
        string? VehicleType,
        bool ForceBridgeCrossing,
        StartLocationType? StartLocationType = null,
        double? StartLatitude = null,
        double? StartLongitude = null,
        bool ReturnToStart = false,
        TimeOnly? DepartureTime = null
    );

    public record RouteStopDto(
        int Order,
        string ProjectCode,
        string ProjectName,
        string? Address,
        double? EstimatedDistanceFromPrevious,
        double? EstimatedDurationFromPrevious,
        double? Latitude = null,
        double? Longitude = null
    );

    public record TimeWindowWarningDto(
        string ProjectCode,
        string ProjectName,
        TimeOnly WindowStart,
        TimeOnly WindowEnd,
        TimeOnly EstimatedArrival,
        bool IsLate,
        string WarningType = "LateArrival"   // "EarlyArrival" | "LateArrival"
    );

    public record RouteOptimizationResultDto(
        List<RouteStopDto> OptimizedStops,
        double TotalDistance,
        double TotalDuration,
        List<string> ExcludedProjects,
        string? BridgeNotice,
        List<TimeWindowWarningDto>? TimeWindowWarnings = null,
        double? StartLatitude = null,
        double? StartLongitude = null
    );

    public record DepotSettingsDto(
        string? DepotName,
        string? DepotAddress,
        double? DepotLatitude,
        double? DepotLongitude
    );
}
