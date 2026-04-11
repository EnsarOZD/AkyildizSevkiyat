using MediatR;

namespace Akyildiz.Sevkiyat.Application.Admin.Commands.ForceCloseDriverSession
{
    public record ForceCloseDriverSessionCommand(Guid SessionId, string? Notes) : IRequest;
}
