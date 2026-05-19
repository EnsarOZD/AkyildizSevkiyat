using Akyildiz.Sevkiyat.Domain.Enums;

namespace Akyildiz.Sevkiyat.Application.Interfaces
{
    public interface ICurrentUserService
    {
        int? UserId { get; }
        UserRole? Role { get; }
        string? Email { get; }
        string? FullName { get; }
    }
}
