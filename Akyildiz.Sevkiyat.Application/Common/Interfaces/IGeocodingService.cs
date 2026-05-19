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
    }
}
