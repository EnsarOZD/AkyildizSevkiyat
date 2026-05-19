namespace Akyildiz.Sevkiyat.Application.Common.Interfaces
{
    public interface IPhotoStorageService
    {
        /// <summary>
        /// Base64 string olarak gelen fotoğrafı diske kaydeder.
        /// Dönen değer: sunucudan erişilebilir göreli yol (örn. "odometer/2025-05/abc.jpg")
        /// </summary>
        Task<string> SaveAsync(string base64, string category, CancellationToken ct = default);

        /// <summary>
        /// Daha önce kaydedilmiş fotoğrafı siler. Hata fırlatmaz (best-effort).
        /// </summary>
        Task DeleteAsync(string relativePath);

        /// <summary>
        /// Göreli yoldan tam URL oluşturur (örn. "/photos/odometer/2025-05/abc.jpg").
        /// </summary>
        string GetUrl(string relativePath);

        /// <summary>
        /// Teslim fotoğrafını yapılandırılmış klasör ve dosya adıyla kaydeder.
        /// Klasör: delivery/yyyy/MM/dd/
        /// Dosya: {shipmentId}_{irsaliyeNo}_{photoIndex}_{shortGuid}.jpg
        /// </summary>
        Task<string> SaveDeliveryPhotoAsync(string base64, int shipmentId, string? irsaliyeNo, int photoIndex, CancellationToken ct = default);
    }
}
