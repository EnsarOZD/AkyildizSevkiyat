using MediatR;

namespace Akyildiz.Sevkiyat.Application.Auth.Commands.Login
{
    public record LoginCommand(string Email, string Password) : IRequest<LoginResponse>;
}
