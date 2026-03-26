using Akyildiz.Sevkiyat.Domain.Entities;

namespace Akyildiz.Sevkiyat.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(User user);
    }
}
