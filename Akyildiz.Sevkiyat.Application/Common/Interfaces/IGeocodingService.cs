namespace Akyildiz.Sevkiyat.Application.Common.Interfaces
{
    public interface IGeocodingService
    {
        /// <summary>
        /// Verilen adresi Google Geocoding API ile koordinata çevirir.
        /// ZERO_RESULTS veya API hatası durumunda null döner.
        /// </summary>
        Task<(double Lat, double Lng)?> GeocodeAsync(string address, CancellationToken cancellationToken = default);
    }
}
