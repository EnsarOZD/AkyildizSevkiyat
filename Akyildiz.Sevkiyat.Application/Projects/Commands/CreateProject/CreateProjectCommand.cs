using MediatR;

namespace Akyildiz.Sevkiyat.Application.Projects.Commands.CreateProject
{
    public record CreateProjectCommand(
        string IssProjectCode,
        string Name,
        string Region
    ) : IRequest<int>;
}
