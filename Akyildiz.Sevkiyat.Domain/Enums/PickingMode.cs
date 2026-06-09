namespace Akyildiz.Sevkiyat.Domain.Enums
{
    /// <summary>Kıyafet sevkiyatının toplama modu (claim sonrası seçilir).</summary>
    public enum PickingMode
    {
        Cart = 0,      // Araba ile — container QR bağlama zorunlu, satırlar QR sonrası açılır
        Pallet = 1,    // Palet ile — container takibi yok; toplama+kapama birleşik, PalletCount girilir
        Handheld = 2   // El ile (poşet vb.) — container takibi yok
    }
}
