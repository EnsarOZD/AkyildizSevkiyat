using System.ComponentModel.DataAnnotations;

namespace Akyildiz.Sevkiyat.Infrastructure.Persistence.Seeding
{
    public sealed class SeedDataOptions
    {
        [Required]
        [MinLength(8, ErrorMessage = "Admin password must be at least 8 characters long.")]
        public string AdminPassword { get; init; } = default!;
    }
}
