using Akyildiz.Sevkiyat.Domain.Enums;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    /// <summary>
    /// Depodaki fiziksel bir konumu temsil eder.
    /// Kod formatı: {KoridorNo}{Taraf}-{ModulNo:D3}-{Kat:D2}[-{InnerLevel}{InnerPosition:D2}]
    /// Örn: "1K-001-03"       → Koridor 1, Kuzey, 1. Modül, 3. Kat (palet raf)
    ///      "2K-020-00-A01"   → Koridor 2, Kuzey, 20. Modül, zemin, A kolu 1. göz (kutu)
    /// </summary>
    public class WarehouseLocation
    {
        public int Id { get; set; }

        /// <summary>Benzersiz konum kodu.</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Koridor numarası. Örn: 1, 2, 3, 4</summary>
        public int KoridorNo { get; set; }

        /// <summary>Taraf: "K" = Kuzey, "G" = Güney</summary>
        public string Taraf { get; set; } = string.Empty;

        /// <summary>Modül numarası. (kodda 3 haneli: 001)</summary>
        public int ModulNo { get; set; }

        /// <summary>Kat (yükseklik). 0 = zemin/toplama gözü seviyesi. (kodda 2 haneli: 00)</summary>
        public int Kat { get; set; }

        /// <summary>
        /// İç konum harfi. Yalnızca Box (kutu) tipinde kullanılır.
        /// Örn: "A", "B" — modül içindeki kol/sıra
        /// </summary>
        public string? InnerLevel { get; set; }

        /// <summary>
        /// İç konum numarası. InnerLevel ile birlikte kullanılır.
        /// Örn: 1, 2, 3 — kol içindeki göz sırası
        /// </summary>
        public int? InnerPosition { get; set; }

        /// <summary>Konteyner tipi: Palet / Koli / Kutu</summary>
        public ContainerType ContainerType { get; set; } = ContainerType.Pallet;

        /// <summary>Opsiyonel açıklama</summary>
        public string? Description { get; set; }

        public LocationType LocationType { get; set; } = LocationType.Rack;

        /// <summary>Maksimum taşıma kapasitesi (kg). Null = sınırsız.</summary>
        public decimal? MaxWeightKg { get; set; }

        /// <summary>Maksimum palet sayısı. Null = sınırsız.</summary>
        public int? MaxPallets { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Toplama gözü alan adı. Yalnızca PickingFace tipi lokasyonlarda kullanılır.
        /// Örn: "Asma Kat Altı", "Soğuk Hava", "Ana Depo"
        /// </summary>
        public string? Alan { get; set; }

        /// <summary>
        /// Rafa yapıştırılan QR kodun değeri (katsız).
        /// Raf için: "1K-001" — terminalde kat seçimi tam adresi tamamlar.
        /// PickingFace için kullanılmaz; Code alanı QR değeri olarak kullanılır.
        /// </summary>
        public string? QrCode { get; set; }

        /// <summary>
        /// Raf toplam kat sayısı. QR okutulduğunda terminalde kat seçim listesi bu sayıya göre oluşur.
        /// Yalnızca Rack ve FloorStack tipleri için geçerlidir.
        /// </summary>
        public int? TotalFloors { get; set; }

        /// <summary>
        /// Kod üretici: {KoridorNo}{Taraf}-{ModulNo:D3}-{Kat:D2}[-{InnerLevel}{InnerPosition:D2}]
        /// InnerLevel boşsa ve InnerPosition varsa salt sayısal iç adres üretir:
        ///   innerLevel="A", innerPosition=1 → "-A01"  (Kutu)
        ///   innerLevel=null, innerPosition=1 → "-01"   (Koli pozisyonu)
        /// </summary>
        public static string BuildCode(int koridorNo, string taraf, int modulNo, int kat,
            string? innerLevel = null, int? innerPosition = null)
        {
            var base_ = $"{koridorNo}{taraf.Trim().ToUpperInvariant()}-{modulNo:D3}-{kat:D2}";
            if (innerPosition.HasValue)
            {
                var lvl = string.IsNullOrWhiteSpace(innerLevel)
                    ? string.Empty
                    : innerLevel.Trim().ToUpperInvariant();
                return $"{base_}-{lvl}{innerPosition.Value:D2}";
            }
            return base_;
        }

        /// <summary>
        /// Toplama gözü kod üretici (eski format, geriye dönük uyumluluk için korunur).
        /// Örn: BuildPickingFaceCode("AKA", 1) → "AKA-001"
        /// </summary>
        public static string BuildPickingFaceCode(string alanKisaltma, int no)
            => $"{alanKisaltma.Trim().ToUpperInvariant()}-{no:D3}";

        /// <summary>
        /// Özel alan kod üretici: Receiving, Returns, FloorStack tipi lokasyonlar için.
        /// Örn: BuildAreaCode("MAL", 1) → "MAL-001", BuildAreaCode("IAD", 2) → "IAD-002"
        /// </summary>
        public static string BuildAreaCode(string prefix, int no)
            => $"{prefix.Trim().ToUpperInvariant()}-{no:D3}";
    }
}
