namespace Akyildiz.Sevkiyat.Domain.Enums
{
    public enum ZonePreparationStatus
    {
        Draft = 0,
        MicroPicking = 1,
        MicroReady = 2,
        MacroPicking = 3,
        GidaHazirlik = 4,       // Gıda ürünleri toplu hazırlık aşaması (aynı zone'un tüm batch'leri birleşir)
        ReadyForDriverInfo = 5, // İrsaliye bekliyor / şoför ataması
        ReadyForTransfer = 6,   // Araca atandı, yüklemeye hazır
        Dispatched = 7          // Yükleme onaylandı, araç yola çıktı
    }
}
