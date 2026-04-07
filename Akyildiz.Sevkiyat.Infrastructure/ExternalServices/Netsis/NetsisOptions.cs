using System.ComponentModel.DataAnnotations;

namespace Akyildiz.Sevkiyat.Infrastructure.ExternalServices.Netsis
{
    public sealed class NetsisOptions
    {
        // Kimlik bilgileri — appsettings'teki SET_BY_ENV_ placeholderları env variable ile ezilir
        [Required]
        public string BaseUrl { get; init; } = default!;

        [Required]
        public string KullaniciAdi { get; init; } = default!;

        [Required]
        public string Sifre { get; init; } = default!;

        [Required]
        public string FirmaKodu { get; init; } = default!;

        [Required]
        public string DbName { get; init; } = default!;

        [Required]
        public string DbUser { get; init; } = default!;

        // DbPassword env variable ile gelir — appsettings'e yazılmaz
        public string DbPassword { get; init; } = string.Empty;

        [Required]
        public string SubeKodu { get; init; } = default!;

        // Opsiyonel
        public string? IsletmeKodu { get; init; }

        // Varsayılan depo kodu — sevkiyat payload'ında kullanılır
        public string? DepoKodu { get; init; }

        public string LoginPath            { get; init; } = "api/v2/token";
        public string SiparisPath          { get; init; } = "api/v2/ItemSlips";
        public string SatinalmaSiparisPath { get; init; } = "api/v2/ItemSlips";
        public string StokBakiyePath       { get; init; } = "api/v2/Queries";
        public string IrsaliyePath         { get; init; } = "api/v2/ItemSlips";

        public int TimeoutSeconds     { get; init; } = 30;
        public int TokenExpiryMinutes { get; init; } = 55;
    }
}
