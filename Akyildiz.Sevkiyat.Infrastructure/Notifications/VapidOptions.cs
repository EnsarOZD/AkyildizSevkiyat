using System.ComponentModel.DataAnnotations;

namespace Akyildiz.Sevkiyat.Infrastructure.Notifications;

public class VapidOptions
{
    [Required]
    public string PublicKey { get; set; } = string.Empty;

    [Required]
    public string PrivateKey { get; set; } = string.Empty;

    // mailto: veya https: URI — Web Push standardı gereği
    [Required]
    public string Subject { get; set; } = string.Empty;
}
