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

        // Satınalma siparişleri için ayrı hesap (procurement@...)
        public string ProcurementFromAddress { get; set; } = string.Empty;
        public string ProcurementFromDisplayName { get; set; } = "Akyıldız Lojistik Satınalma";
        public string ProcurementUserName { get; set; } = string.Empty;
        public string ProcurementPassword { get; set; } = string.Empty;

        // Dolu ise eksik/kısmi gönderim bildirimleri gerçek alıcı yerine bu adrese gider (test amaçlı)
        public string DispatchTestRecipient { get; set; } = string.Empty;

        // Shared-hosting sertifika uyumsuzluğu için (ör. Natro)
        public bool SkipCertificateValidation { get; set; } = false;
    }
}
