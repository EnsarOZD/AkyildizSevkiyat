using MediatR;

namespace Akyildiz.Sevkiyat.Application.Projects.Commands.UpdateProject
{
    public record UpdateProjectCommand(
        int Id,
        string Code,
        string Name,
        string Region,
        bool IsActive,
        int? DeliveryOrder = null
    ) : IRequest<Unit>;
}
