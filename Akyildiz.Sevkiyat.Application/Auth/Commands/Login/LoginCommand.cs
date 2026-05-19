using MediatR;

namespace Akyildiz.Sevkiyat.Application.Auth.Commands.Login
{
    public record LoginCommand(string Username, string Password, bool RememberMe = false) : IRequest<LoginResponse>;
}
