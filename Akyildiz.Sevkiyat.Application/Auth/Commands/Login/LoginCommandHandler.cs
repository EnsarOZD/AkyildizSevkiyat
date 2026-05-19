using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using RefreshTokenEntity = Akyildiz.Sevkiyat.Domain.Entities.RefreshToken;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace Akyildiz.Sevkiyat.Application.Auth.Commands.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly int _refreshTokenExpiryHours;
        private readonly int _rememberMeExpiryHours;

        public LoginCommandHandler(
            IApplicationDbContext context,
            IPasswordHasher passwordHasher,
            ITokenService tokenService,
            IConfiguration configuration)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _refreshTokenExpiryHours = int.TryParse(configuration["Jwt:RefreshTokenExpiryHours"], out var h) ? h : 8;
            _rememberMeExpiryHours = int.TryParse(configuration["Jwt:RememberMeExpiryHours"], out var r) ? r : 168;
        }

        public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Username, cancellationToken);

            if (user == null)
                throw new UnauthorizedException("Kullanıcı adı veya şifre hatalı.");

            if (!user.IsActive)
                throw new UnauthorizedException("Kullanıcı hesabı aktif değil.");

            if (!_passwordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
                throw new UnauthorizedException("Kullanıcı adı veya şifre hatalı.");

            var accessToken = _tokenService.GenerateToken(user);

            // Refresh token — raw token client'a gönderilir, hash'i DB'de saklanır
            var rawRefreshToken = GenerateRawToken();
            var tokenHash = HashToken(rawRefreshToken);

            var expiryHours = request.RememberMe ? _rememberMeExpiryHours : _refreshTokenExpiryHours;
            _context.RefreshTokens.Add(new RefreshTokenEntity
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = tokenHash,
                ExpiresAt = DateTime.UtcNow.AddHours(expiryHours),
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(cancellationToken);

            return new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = rawRefreshToken,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Username = user.Username,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role.ToString()
                }
            };
        }

        private static string GenerateRawToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes);
        }

        internal static string HashToken(string rawToken)
        {
            var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
