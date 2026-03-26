namespace Akyildiz.Sevkiyat.Application.Interfaces
{
    public interface IPasswordHasher
    {
        string CreateHash(string password, out string salt);
        bool Verify(string password, string hash, string salt);
    }
}
