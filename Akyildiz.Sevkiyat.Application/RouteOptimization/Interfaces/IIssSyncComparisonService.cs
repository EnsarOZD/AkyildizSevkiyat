using Akyildiz.Sevkiyat.Application.RouteOptimization.Dtos;

namespace Akyildiz.Sevkiyat.Application.RouteOptimization.Interfaces
{
    public interface IIssSyncComparisonService
    {
        Task<List<ProjectSyncComparisonDto>> CompareWithIssAsync(
            List<string> projectCodes,
            CancellationToken cancellationToken = default);
    }
}
