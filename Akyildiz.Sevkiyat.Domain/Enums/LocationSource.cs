namespace Akyildiz.Sevkiyat.Domain.Enums
{
    /// <summary>
    /// Bir projenin kayıtlı koordinatının kaynağı/güvenilirliği.
    /// Veri kalitesi raporunda "şoför doğruladı" olanlar otorite kabul edilir.
    /// </summary>
    public enum LocationSource
    {
        None           = 0,  // Koordinat yok / kaynağı bilinmiyor
        Geocoded       = 1,  // Adresten Google geocode ile bulundu (tahmin)
        DriverVerified = 2,  // Şoför yerinde "Konumu Kaydet" ile aldı (en güvenilir)
        Manual         = 3   // Elle girildi (yönetim)
    }
}
