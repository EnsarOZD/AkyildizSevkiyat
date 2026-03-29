using Akyildiz.Sevkiyat.Application.Auth.Commands.Login;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.Auth.Commands.RefreshToken
{
    public record RefreshTokenCommand(string RefreshToken) : IRequest<LoginResponse>;
}
