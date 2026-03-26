using MediatR;

namespace Akyildiz.Sevkiyat.Application.Projects.Commands.DeleteProject
{
    public record DeleteProjectCommand(int Id) : IRequest<Unit>;
}
