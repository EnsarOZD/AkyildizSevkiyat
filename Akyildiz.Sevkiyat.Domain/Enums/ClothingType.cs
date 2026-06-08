namespace Akyildiz.Sevkiyat.Domain.Enums
{
    /// <summary>
    /// Kıyafet operasyonunda toplama gruplaması için stok alt-türü.
    /// Yalnızca Category == Kiyafet olan stoklar için anlamlıdır.
    /// </summary>
    public enum ClothingType
    {
        Diger = 0,      // Giysi / diğer
        Ayakkabi = 1
    }
}
