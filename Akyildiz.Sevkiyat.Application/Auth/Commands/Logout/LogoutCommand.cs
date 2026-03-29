using Akyildiz.Sevkiyat.Application.Auth.Commands.Login;
using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Auth.Commands.Logout
{
    public record LogoutCommand(string RefreshToken) : IRequest<Unit>;

    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Unit>
    {
        private readonly IApplicationDbContext _context;

        public LogoutCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return Unit.Value; // Zaten oturum yok, idempotent

            var tokenHash = LoginCommandHandler.HashToken(request.RefreshToken);

            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

            if (token != null && !token.IsRevoked)
                token.Revoke();

            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
