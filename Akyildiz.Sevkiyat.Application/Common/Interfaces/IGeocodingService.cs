namespace Akyildiz.Sevkiyat.Application.Common.Interfaces
{
    public record GeocodedLocation(
        double Lat,
        double Lng,
        string? CityName,
        string? DistrictName
    );

    public interface IGeocodingService
    {
        /// <summary>
        /// Verilen adresi Google Geocoding API ile koordinata çevirir.
        /// ZERO_RESULTS veya API hatası durumunda null döner.
        /// </summary>
        Task<GeocodedLocation?> GeocodeAsync(string address, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verilen koordinatı (lat/lng) Google ters-geocode ile il/ilçeye çevirir.
        /// Şoför-doğrulanmış koordinatın gerçek il/ilçesini bulmak için kullanılır.
        /// API hatası durumunda null döner.
        /// </summary>
        Task<GeocodedLocation?> ReverseGeocodeAsync(double lat, double lng, CancellationToken cancellationToken = default);

        /// <summary>
        /// İsim/işletme adını Google Places "Find Place" ile koordinata çevirir
        /// (örn. "ZORLU CENTER İSTANBUL BEŞİKTAŞ"). Adres metni bozuk/eksik projelerde
        /// çapraz kontrol için kullanılır. Bulunamazsa null döner.
        /// </summary>
        Task<GeocodedLocation?> PlaceSearchAsync(string query, CancellationToken cancellationToken = default);
    }
}
