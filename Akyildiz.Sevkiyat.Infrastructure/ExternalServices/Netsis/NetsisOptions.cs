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
        public string SubeKodu { get; init; } = default!;

        // Opsiyonel
        public string? IsletmeKodu { get; init; }

        // Varsayılan depo kodu — sevkiyat payload'ında kullanılır
        public string? DepoKodu { get; init; }

        // TODO: NETSIS_API — Aşağıdaki path'ler API dokümantasyonu gelince güncellenir.
        // "PENDING_NETSIS_API_DOCS" değeri görülürse entegrasyon henüz yapılandırılmamış demektir.
        public string LoginPath              { get; init; } = "PENDING_NETSIS_API_DOCS";
        public string SiparisPath            { get; init; } = "PENDING_NETSIS_API_DOCS"; // Müşteri siparişi
        public string SatinalmaSiparisPath   { get; init; } = "PENDING_NETSIS_API_DOCS"; // Satınalma siparişi
        public string StokBakiyePath         { get; init; } = "PENDING_NETSIS_API_DOCS"; // Stok bakiye sync
        public string IrsaliyePath           { get; init; } = "PENDING_NETSIS_API_DOCS"; // İrsaliye okuma

        public int TimeoutSeconds      { get; init; } = 30;
        public int TokenExpiryMinutes  { get; init; } = 55; // gerçek token süresinden ~5dk kısa tut
    }
}
