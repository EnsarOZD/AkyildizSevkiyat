using System.ComponentModel.DataAnnotations;

namespace Akyildiz.Sevkiyat.Infrastructure.Email
{
    public class SmtpOptions
    {
        [Required]
        public string Host { get; set; } = string.Empty;

        [Range(1, 65535)]
        public int Port { get; set; } = 587;

        public bool EnableSsl { get; set; } = true;

        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string FromAddress { get; set; } = string.Empty;

        public string FromDisplayName { get; set; } = "Akyıldız Sevkiyat";
    }
}
