using Akyildiz.Sevkiyat.Application.Common.Dtos;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.Projects.Queries
{
    public record GetAllProjectsQuery() : IRequest<List<ProjectDto>>;
}
