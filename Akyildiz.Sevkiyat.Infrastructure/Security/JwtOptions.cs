using System.ComponentModel.DataAnnotations;

namespace Akyildiz.Sevkiyat.Infrastructure.Security
{
    public sealed class JwtOptions
    {
        [Required]
        [MinLength(32, ErrorMessage = "JWT Key must be at least 32 characters long.")]
        public string Key { get; init; } = default!;

        [Required]
        public string Issuer { get; init; } = default!;

        [Required]
        public string Audience { get; init; } = default!;

        [Range(1, 10080)] // 1 minute to 1 week
        public int ExpiresInMinutes { get; init; } = 60;
    }
}
