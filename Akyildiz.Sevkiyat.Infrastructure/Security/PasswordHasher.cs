using Akyildiz.Sevkiyat.Application.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace Akyildiz.Sevkiyat.Infrastructure.Security
{
    public class PasswordHasher : IPasswordHasher
    {
        private const int KeySize = 64;
        private const int Iterations = 350000;
        private readonly HashAlgorithmName _hashAlgorithm = HashAlgorithmName.SHA512;

        public string CreateHash(string password, out string salt)
        {
            var saltBytes = RandomNumberGenerator.GetBytes(KeySize);
            salt = Convert.ToBase64String(saltBytes);

            var hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                saltBytes,
                Iterations,
                _hashAlgorithm,
                KeySize);

            return Convert.ToBase64String(hash);
        }

        public bool Verify(string password, string hash, string salt)
        {
            var saltBytes = Convert.FromBase64String(salt);
            var hashToCompare = Rfc2898DeriveBytes.Pbkdf2(
                password,
                saltBytes,
                Iterations,
                _hashAlgorithm,
                KeySize);

            return CryptographicOperations.FixedTimeEquals(
                hashToCompare,
                Convert.FromBase64String(hash));
        }
    }
}
