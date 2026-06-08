namespace Akyildiz.Sevkiyat.Domain.Entities
{
    /// <summary>
    /// Kıyafet toplama ekranında stok adında vurgulanacak anahtar kelimeler
    /// (örn. Kısakol, Uzunkol, Reflektörlü, Güvenlik). Eşleşme boşluk/yazım
    /// farklarına dayanıklı yapılır (kısakol = kısa kol).
    /// </summary>
    public class ClothingHighlightKeyword
    {
        public int Id { get; set; }
        public string Keyword { get; set; } = string.Empty;
        public string Color { get; set; } = "#ef4444"; // hex renk (rozet arka planı)
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; } = 0;
    }
}
