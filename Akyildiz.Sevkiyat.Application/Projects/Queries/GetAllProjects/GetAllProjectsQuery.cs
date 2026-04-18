using Akyildiz.Sevkiyat.Application.Common.Dtos;
using Akyildiz.Sevkiyat.Application.Common.Models;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.Projects.Queries
{
    public record GetAllProjectsQuery(
        int PageNumber = 1,
        int PageSize = 50,
        string? Search = null,
        bool ShowInactive = false
    ) : IRequest<PaginatedList<ProjectDto>>;
}
