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
        string? VehicleType,         // "Kamyon" | "Kamyonet" | "Minibus"
        bool ForceBridgeCrossing     // inject bridge as mandatory waypoint
    );

    public record RouteStopDto(
        int Order,
        string ProjectCode,
        string ProjectName,
        string? Address,
        double? EstimatedDistanceFromPrevious,
        double? EstimatedDurationFromPrevious
    );

    public record RouteOptimizationResultDto(
        List<RouteStopDto> OptimizedStops,
        double TotalDistance,
        double TotalDuration,
        List<string> ExcludedProjects
    );
}
