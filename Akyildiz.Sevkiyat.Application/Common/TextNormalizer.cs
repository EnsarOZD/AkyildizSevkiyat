using System.Text;
using System.Globalization;

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
            ('İ', 'I'), ('ı', 'I'),
            ('Ğ', 'G'), ('ğ', 'G'),
            ('Ş', 'S'), ('ş', 'S'),
            ('Ç', 'C'), ('ç', 'C'),
            ('Ö', 'O'), ('ö', 'O'),
            ('Ü', 'U'), ('ü', 'U'),
        };

        /// <summary>
        /// Metni karşılaştırma için normalleştirir:
        /// 1. Türkçe karakterleri İngilizce karşılıklarına çevirir (İ->I, ş->s vb.)
        /// 2. Unicode diakritiği kaldırır (é->e, â->a vb.)
        /// 3. Büyük harfe çevirir (ToUpperInvariant)
        /// 4. Boşlukları temizler
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

            // 2. Unicode normalizasyon (é->e, â->a vb.) ve Diakritik filtreleme
            var normalized = sb.ToString().Normalize(NormalizationForm.FormD);
            var result = new StringBuilder(normalized.Length);
            
            foreach (var ch in normalized)
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (cat != UnicodeCategory.NonSpacingMark)
                {
                    // 3. Büyük harfe çevir
                    result.Append(char.ToUpperInvariant(ch));
                }
            }

            // 4. Boşlukları temizle
            return string.Join(' ', result.ToString()
                                          .Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
