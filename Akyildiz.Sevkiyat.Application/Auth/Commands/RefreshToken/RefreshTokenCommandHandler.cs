using Akyildiz.Sevkiyat.Application.Auth.Commands.Login;

namespace Akyildiz.Sevkiyat.Application.Auth.Commands.RefreshToken
{
    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, LoginResponse>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _context;
        private readonly ITokenService _tokenService;

        public RefreshTokenCommandHandler(
            ICurrentUserService currentUserService,
            IApplicationDbContext context,
            ITokenService tokenService)
        {
            _currentUserService = currentUserService;
            _context = context;
            _tokenService = tokenService;
        }

        public async Task<LoginResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId
                ?? throw new UnauthorizedException("Kullanıcı kimliği doğrulanamadı.");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                ?? throw new UnauthorizedException("Kullanıcı bulunamadı.");

            if (!user.IsActive)
            {
                throw new UnauthorizedException("Kullanıcı hesabı aktif değil.");
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
