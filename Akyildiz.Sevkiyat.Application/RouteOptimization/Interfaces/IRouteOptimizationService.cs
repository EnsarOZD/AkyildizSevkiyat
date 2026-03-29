using Akyildiz.Sevkiyat.Application.RouteOptimization.Dtos;

namespace Akyildiz.Sevkiyat.Application.RouteOptimization.Interfaces
{
    public interface IRouteOptimizationService
    {
        Task<RouteOptimizationResultDto> OptimizeRouteAsync(
            RouteOptimizationRequestDto request,
            List<string> addresses,
            List<string> projectCodes,
            List<string> projectNames,
            List<double?> latitudes,
            List<double?> longitudes,
            List<TimeOnly?> deliveryWindowStarts,
            List<TimeOnly?> deliveryWindowEnds,
            CancellationToken cancellationToken = default);
    }
}
