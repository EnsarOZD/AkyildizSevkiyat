using Akyildiz.Sevkiyat.Application.RouteOptimization.Dtos;

namespace Akyildiz.Sevkiyat.Application.RouteOptimization.Interfaces
{
    public interface IRouteOptimizationService
    {
        Task<RouteOptimizationResultDto> OptimizeRouteAsync(
            List<string> addresses,
            string? startAddress,
            List<string> projectCodes,
            CancellationToken cancellationToken = default);
    }
}
