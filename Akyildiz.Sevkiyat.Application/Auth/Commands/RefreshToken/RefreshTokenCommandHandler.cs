using Akyildiz.Sevkiyat.Application.Auth.Commands.Login;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Akyildiz.Sevkiyat.Application.Auth.Commands.RefreshToken
{
    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, LoginResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly ITokenService _tokenService;

        public RefreshTokenCommandHandler(IApplicationDbContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        public async Task<LoginResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                throw new UnauthorizedException("Refresh token gereklidir.");

            var tokenHash = LoginCommandHandler.HashToken(request.RefreshToken);

            var existing = await _context.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

            if (existing == null || !existing.IsActive)
                throw new UnauthorizedException("Geçersiz veya süresi dolmuş refresh token.");

            if (!existing.User.IsActive)
                throw new UnauthorizedException("Kullanıcı hesabı aktif değil.");

            // Token rotation — eskiyi iptal et
            existing.Revoke();

            // Yeni access token + yeni refresh token
            var newAccessToken = _tokenService.GenerateToken(existing.User);

            var rawNewRefresh = GenerateRawToken();
            var newHash = LoginCommandHandler.HashToken(rawNewRefresh);

            _context.RefreshTokens.Add(new Domain.Entities.RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = existing.UserId,
                TokenHash = newHash,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(cancellationToken);

            return new LoginResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = rawNewRefresh,
                User = new UserDto
                {
                    Id = existing.User.Id,
                    Email = existing.User.Email,
                    FirstName = existing.User.FirstName,
                    LastName = existing.User.LastName,
                    Role = existing.User.Role.ToString()
                }
            };
        }

        private static string GenerateRawToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes);
        }
    }
}
