using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    public class StockMaster
    {
        public int Id { get; set; }

        public string StockCode { get; set; } = string.Empty; // Unique Index
        public string StockName { get; set; } = string.Empty;
        public string? Brand { get; set; }

        public Akyildiz.Sevkiyat.Domain.Enums.StockUnit Unit { get; set; } = Akyildiz.Sevkiyat.Domain.Enums.StockUnit.Adet;
        public decimal UnitPrice { get; set; }
        public Akyildiz.Sevkiyat.Domain.Enums.TaxRate TaxRate { get; set; } = Akyildiz.Sevkiyat.Domain.Enums.TaxRate.Percent20;
        public Akyildiz.Sevkiyat.Domain.Enums.StockCategory Category { get; set; } = Akyildiz.Sevkiyat.Domain.Enums.StockCategory.Tanimsiz;

        /// <summary>
        /// Kıyafet operasyonu toplama gruplaması (Ayakkabı / Diğer). Yalnızca Category == Kiyafet
        /// için anlamlıdır; boş bırakılabilir (varsayılan: Diğer gibi davranır).
        /// </summary>
        public Akyildiz.Sevkiyat.Domain.Enums.ClothingType? ClothingType { get; set; }
        public Akyildiz.Sevkiyat.Domain.Enums.PickingType PickingType { get; set; } = Akyildiz.Sevkiyat.Domain.Enums.PickingType.Unassigned;

        // Stok seviyeleri — sadece domain metodları üzerinden değiştirilmeli
        public decimal OnHandQty  { get; private set; } = 0;
        public decimal ReservedQty { get; private set; } = 0;
        public decimal AvailableQty => OnHandQty - ReservedQty;

        // Kritik stok eşiği
        public decimal? MinStockQty { get; set; }

        // Sipariş ver eşiği — bu seviyeye düşünce PO önerilir (MinStockQty'den küçük olmalı)
        public decimal? ReorderPoint { get; set; }

        // Depo lokasyonu (örn. "A-01-03")
        public string? WarehouseLocation { get; set; }

        // Netsis stok kodu — API bilgileri gelince doldurulur
        public string? NetsisStockCode { get; set; }

        /// <summary>
        /// Birincil tedarikçi barkodu (EAN13, Code128 vb.).
        /// Hızlı lookup için. Birden fazla barkod için StockBarcode tablosunu kullanın.
        /// </summary>
        public string? Barcode { get; set; }

        /// <summary>
        /// Bu ürünün sabit toplama gözü (PickingFace lokasyonu).
        /// Picking yönlendirmesinde öncelikli olarak bu lokasyon önerilir.
        /// </summary>
        public int? DefaultPickingFaceId { get; set; }
        public WarehouseLocation? DefaultPickingFace { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Pick listelerinde sıralama için kullanılır. Düşük değer önce gelir.
        /// </summary>
        public int PickingOrder { get; set; } = 0;

        /// <summary>
        /// Ürünün birim ağırlığı (kg). Toplam tonaj hesabında kullanılır. Boş bırakılabilir.
        /// </summary>
        public decimal? WeightKg { get; set; }

        // EF Core optimistic concurrency token
        public byte[] RowVersion { get; set; } = null!;

        // ── Domain Metodları ──────────────────────────────────────────────────

        /// <summary>
        /// Stok rezervasyonu — AssignToWarehouse sırasında çağrılır.
        /// AvailableQty kontrolü yapar, yetersizse DomainException fırlatır.
        /// </summary>
        public void Reserve(decimal qty)
        {
            if (qty <= 0)
                throw new DomainException($"{StockCode}: rezervasyon miktarı pozitif olmalı.");
            if (AvailableQty < qty)
                throw new DomainException(
                    $"{StockCode}: yetersiz stok. Mevcut: {AvailableQty}, Talep: {qty}");
            ReservedQty += qty;
        }

        /// <summary>
        /// Rezervasyon iptali — sevkiyat iptal edildiğinde çağrılır.
        /// </summary>
        public void ReleaseReservation(decimal qty)
        {
            if (qty <= 0) return;
            ReservedQty = Math.Max(0, ReservedQty - qty);
        }

        /// <summary>
        /// Stok çıkışı — Delivered sırasında çağrılır.
        /// OnHandQty ve ReservedQty'yi birlikte düşürür.
        /// </summary>
        public void Deduct(decimal qty)
        {
            if (qty <= 0) return;
            if (OnHandQty < qty)
                throw new DomainException(
                    $"{StockCode}: yetersiz stok. Mevcut: {OnHandQty}, Çıkış: {qty}");
            OnHandQty  -= qty;
            ReservedQty = Math.Max(0, ReservedQty - qty);
        }

        /// <summary>
        /// Stok girişi — GoodsReceipt ve iade sırasında çağrılır.
        /// </summary>
        public void Increase(decimal qty)
        {
            if (qty <= 0)
                throw new DomainException($"{StockCode}: artış miktarı pozitif olmalı.");
            OnHandQty += qty;
        }

        /// <summary>
        /// Stok sayım düzeltmesi — diff pozitif veya negatif olabilir.
        /// Sonuç negatife düşüyorsa DomainException fırlatır.
        /// </summary>
        public void AdjustOnHand(decimal diff)
        {
            var newQty = OnHandQty + diff;
            if (newQty < 0)
                throw new DomainException(
                    $"{StockCode}: stok sayım düzeltmesi negatif bakiye oluşturur. " +
                    $"Mevcut: {OnHandQty}, Fark: {diff}");
            OnHandQty = newQty;
        }

        /// <summary>
        /// Dış sistem senkronizasyonu (Netsis) — bakiyeyi doğrudan yazar.
        /// </summary>
        public void OverrideOnHand(decimal newQty)
        {
            if (newQty < 0)
                throw new DomainException(
                    $"{StockCode}: dış sistem senkronizasyonu negatif bakiye oluşturamazThis.");
            OnHandQty = newQty;
        }
    }
}
