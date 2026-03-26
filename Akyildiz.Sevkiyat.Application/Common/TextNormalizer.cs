using System.Text;

namespace Akyildiz.Sevkiyat.Application.Common
{
    /// <summary>
    /// Stok adı karşılaştırmalarında kullanılan metin normalizasyonu.
    /// Türkçe karakterleri ASCII karşılıklarına dönüştürür, küçük harfe çevirir ve boşlukları temizler.
    /// Amaç: "ÇAMAŞIR SUYU" == "camasir suyu" == "Çamaşır Suyu" → hepsi "camasir suyu"
    /// </summary>
    public static class TextNormalizer
    {
        private static readonly (char From, char To)[] TurkishMap =
        {
            ('İ', 'i'), ('ı', 'i'),
            ('Ğ', 'g'), ('ğ', 'g'),
            ('Ş', 's'), ('ş', 's'),
            ('Ç', 'c'), ('ç', 'c'),
            ('Ö', 'o'), ('ö', 'o'),
            ('Ü', 'u'), ('ü', 'u'),
        };

        /// <summary>
        /// Metni karşılaştırma için normalleştirir:
        /// 1. Türkçe karakterleri ASCII'ye çevirir
        /// 2. Unicode diakritiği kaldırır (é→e, â→a vb.)
        /// 3. Küçük harfe çevirir
        /// 4. Baştaki/sondaki boşlukları siler, çoklu boşlukları teke indirir
        /// </summary>
        public static string NormalizeForComparison(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // 1. Türkçe karakter dönüşümü
            var sb = new StringBuilder(input.Length);
            foreach (var ch in input)
            {
                var mapped = ch;
                foreach (var (from, to) in TurkishMap)
                {
                    if (ch == from) { mapped = to; break; }
                }
                sb.Append(mapped);
            }

            // 2. Unicode normalizasyon (é→e, â→a vb.)
            var normalized = sb.ToString()
                               .Normalize(NormalizationForm.FormD);

            // 3. Diakriti işaretlerini filtrele, küçük harfe çevir
            var result = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (cat != UnicodeCategory.NonSpacingMark)
                    result.Append(char.ToLowerInvariant(ch));
            }

            // 4. Boşlukları temizle
            return string.Join(' ', result.ToString()
                                          .Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
