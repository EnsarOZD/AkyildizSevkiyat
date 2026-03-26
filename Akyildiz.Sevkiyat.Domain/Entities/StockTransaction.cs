using Akyildiz.Sevkiyat.Domain.Enums;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    public class StockTransaction
    {
        public int Id { get; set; }

        public int StockMasterId { get; set; }
        public StockMaster StockMaster { get; set; } = null!;

        public StockTransactionType Type { get; set; }

        /// <summary>
        /// Pozitif = giriş, Negatif = çıkış
        /// </summary>
        public decimal Qty { get; set; }

        /// <summary>
        /// Kaynak belge referansı: WaybillNo, TalepNo, vs.
        /// </summary>
        public string? Reference { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow;

        public string? Note { get; set; }
    }
}
