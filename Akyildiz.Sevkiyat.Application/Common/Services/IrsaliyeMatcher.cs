namespace Akyildiz.Sevkiyat.Application.Common.Services
{
    /// <summary>
    /// İrsaliye numarası eşleştirme. GİB e-İrsaliye karekodu (16 hane) ile Netsis'in sakladığı
    /// numara (15 hane) dolgudaki sıfır sayısı yüzünden farklı olabiliyor
    /// (örn. QR "AKI2026000009554" ↔ Netsis "AKI202600009554"). Önce birebir, eşleşmezse
    /// sıfırları yok sayarak karşılaştırırız. Karşılaştırma yalnızca seferin az sayıdaki
    /// sevkiyatına karşı yapıldığından sıfır-duyarsız eşleşme pratikte güvenlidir.
    /// </summary>
    public static class IrsaliyeMatcher
    {
        /// <summary>Harf/rakam dışını atar, büyük harfe çevirir.</summary>
        public static string Normalize(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            return new string(raw.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        }

        public static bool Matches(string? a, string? b)
        {
            var na = Normalize(a);
            var nb = Normalize(b);
            if (na.Length == 0 || nb.Length == 0) return false;
            if (na == nb) return true;
            // Dolgu sıfırı farkını tolere et
            return na.Replace("0", "") == nb.Replace("0", "");
        }
    }
}
