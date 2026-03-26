using System.ComponentModel.DataAnnotations;

namespace Akyildiz.Sevkiyat.Infrastructure.ExternalServices.IssIp
{
    public sealed class ISSIpOptions
    {
        [Required]
        public string BaseUrl { get; init; } = "https://generalapi.issturkiye.com/";
        
        [Required]
        public string EndpointPath { get; init; } = "/";
        
        [Required]
        public string KullaniciAdi { get; init; } = default!;
        
        [Required]
        public string Sifre { get; init; } = default!;
        
        [Required]
        public string BasicAuthUsername { get; init; } = default!;

        [Required]
        public string BasicAuthPassword { get; init; } = default!;
        public int TimeoutSeconds { get; init; } = 30;
    }
}
