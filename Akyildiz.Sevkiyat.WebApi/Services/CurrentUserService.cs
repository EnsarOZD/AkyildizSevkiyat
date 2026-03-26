using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using System.Security.Claims;
using System.Security.Principal;
using System.IdentityModel.Tokens.Jwt;

namespace Akyildiz.Sevkiyat.WebApi.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int? UserId
        {
            get
            {
                // sub or nameidentifier
                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                    ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                
                if (string.IsNullOrEmpty(userIdClaim)) return null;
                return int.TryParse(userIdClaim, out var userId) ? userId : null;
            }
        }

        public UserRole? Role
        {
            get
            {
                // role or http://schemas.microsoft.com/ws/2008/06/identity/claims/role
                var roleClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role)
                    ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("role");
                
                if (string.IsNullOrEmpty(roleClaim)) return null;

                // ignoreCase = true prevents "admin" vs "Admin" issues
                return Enum.TryParse<UserRole>(roleClaim, true, out var role) ? role : null;
            }
        }

        public string? Email => _httpContextAccessor.HttpContext?.User?.FindFirstValue(JwtRegisteredClaimNames.Email);
    }
}
