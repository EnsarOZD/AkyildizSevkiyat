namespace Akyildiz.Sevkiyat.Domain.Enums
{
    /// <summary>Eksik ürün kaydının yaşam döngüsü.</summary>
    public enum ShortageStatus
    {
        Pending = 0,            // Eksik kaydedildi, beklemede
        DispatchRequested = 1,  // "Gönder" denildi, tamamlama sevkiyatı oluşturuldu
        Picked = 2,             // Tamamlama toplandı
        Shipped = 3,            // Tamamlama gönderildi
        Cancelled = 9
    }
}
