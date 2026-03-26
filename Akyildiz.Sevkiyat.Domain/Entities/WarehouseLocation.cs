using Akyildiz.Sevkiyat.Domain.Enums;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    /// <summary>
    /// Depodaki fiziksel bir konumu temsil eder.
    /// Kod formatı: {KoridorNo}{Taraf}-{ModulNo:D3}-{Kat:D2}
    /// Örn: "1K-001-03" → Koridor 1, Kuzey, 1. Modül, 3. Kat
    ///      "2G-005-02" → Koridor 2, Güney, 5. Modül, 2. Kat
    /// </summary>
    public class WarehouseLocation
    {
        public int Id { get; set; }

        /// <summary>Benzersiz konum kodu. Örn: "1K-001-03"</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Koridor numarası. Örn: 1, 2, 3, 4</summary>
        public int KoridorNo { get; set; }

        /// <summary>Taraf: "K" = Kuzey, "G" = Güney</summary>
        public string Taraf { get; set; } = string.Empty;

        /// <summary>Modül numarası. Örn: 1, 2, 3 ... (kodda 3 haneli: 001)</summary>
        public int ModulNo { get; set; }

        /// <summary>Kat (yükseklik). 1 = zemin seviyesi (kodda 2 haneli: 01)</summary>
        public int Kat { get; set; }

        /// <summary>Opsiyonel açıklama</summary>
        public string? Description { get; set; }

        public LocationType LocationType { get; set; } = LocationType.Rack;

        /// <summary>Maksimum taşıma kapasitesi (kg). Null = sınırsız.</summary>
        public decimal? MaxWeightKg { get; set; }

        /// <summary>Maksimum palet sayısı. Null = sınırsız.</summary>
        public int? MaxPallets { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Kod üretici yardımcı metodu.
        /// </summary>
        public static string BuildCode(int koridorNo, string taraf, int modulNo, int kat)
            => $"{koridorNo}{taraf.Trim().ToUpperInvariant()}-{modulNo:D3}-{kat:D2}";
    }
}
