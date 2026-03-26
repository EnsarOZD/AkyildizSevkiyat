namespace Akyildiz.Sevkiyat.Domain.Entities
{
    public class StockCountLine
    {
        public int Id { get; set; }

        public int StockCountId { get; set; }
        public StockCount StockCount { get; set; } = null!;

        public int StockMasterId { get; set; }
        public StockMaster StockMaster { get; set; } = null!;

        /// <summary>
        /// Sayım başladığındaki sistem stoku (snapshot)
        /// </summary>
        public decimal ExpectedQty { get; set; }

        /// <summary>
        /// Depocu tarafından fiziksel sayım sonucu girilen miktar
        /// null = henüz sayılmadı
        /// </summary>
        public decimal? ActualQty { get; set; }

        /// <summary>
        /// ActualQty - ExpectedQty (computed, pozitif = fazla, negatif = eksik)
        /// </summary>
        public decimal? DifferenceQty => ActualQty.HasValue ? ActualQty.Value - ExpectedQty : null;

        public string? Note { get; set; }
    }
}
