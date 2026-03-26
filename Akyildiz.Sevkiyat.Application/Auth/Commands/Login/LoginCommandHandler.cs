using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Auth.Commands.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;

        public LoginCommandHandler(
            IApplicationDbContext context,
            IPasswordHasher passwordHasher,
            ITokenService tokenService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
        }

        public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

            if (user == null)
            {
                throw new UnauthorizedException("Kullanıcı adı veya şifre hatalı.");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedException("Kullanıcı hesabı aktif değil.");
            }

            if (!_passwordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
            {
                throw new UnauthorizedException("Kullanıcı adı veya şifre hatalı.");
            }

            var token = _tokenService.GenerateToken(user);

            return new LoginResponse
            {
                AccessToken = token,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role.ToString()
                }
            };
        }
    }
}
