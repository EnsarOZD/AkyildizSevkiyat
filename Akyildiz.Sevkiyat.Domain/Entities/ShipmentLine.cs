using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    public class ShipmentLine
    {
        public int Id { get; private set; }

        public int ShipmentId { get; private set; }
        public Shipment Shipment { get; private set; } = null!;

        public int? IssOrderLineId { get; internal set; }
        public IssOrderLine? IssOrderLine { get; internal set; }

        public int? StockMasterId { get; internal set; }
        public StockMaster? StockMaster { get; internal set; }

        public string StockCode { get; private set; } = null!;
        public string StockName { get; private set; } = null!;
        public StockUnit Unit { get; private set; } = StockUnit.Adet;

        public decimal OrderedQty { get; private set; }      // ISS sipariş miktarı (kopya)
        public decimal DeliveredQty { get; private set; }    // Depo fiili miktar

        public decimal DifferenceQty => DeliveredQty - OrderedQty;

        /// <summary>
        /// Teslimde stoktan düşülecek net miktar: araca yüklenen (toplanan; toplama yoksa
        /// sipariş) miktardan, teslimden önce kaydedilmiş iadeler düşülür. İade edilen kalem
        /// teslim/çıkış sayılmaz. Negatife düşmez (tamamı iade edilmişse 0).
        /// Hem tekli (MarkShipmentDelivered) hem toplu (BulkMarkShipmentsDelivered) teslim
        /// akışında kullanılır.
        /// </summary>
        public decimal NetDeliveredQty
        {
            get
            {
                var loaded = DeliveredQty > 0 ? DeliveredQty : OrderedQty;
                var net = loaded - (ReturnedQty ?? 0);
                return net > 0 ? net : 0;
            }
        }

        public string? DifferenceReason { get; private set; }
        public string? Note { get; private set; }

        // Araç dönüşü
        public decimal? ReturnedQty { get; private set; }
        public ReturnReason? ReturnReason { get; private set; }

        protected ShipmentLine() { }

        public static ShipmentLine CreateWithEntities(IssOrderLine? issOrderLine, StockMaster? stockMaster,
            string stockCode, string stockName, StockUnit unit, decimal orderedQty)
        {
            var line = new ShipmentLine
            {
                StockCode = stockCode,
                StockName = stockName,
                Unit = unit,
                OrderedQty = orderedQty
            };
            line.IssOrderLine = issOrderLine;
            line.StockMaster = stockMaster;
            return line;
        }

        public static ShipmentLine Create(int? issOrderLineId, int? stockMasterId,
            string stockCode, string stockName, StockUnit unit, decimal orderedQty)
        {
            var line = new ShipmentLine
            {
                StockCode = stockCode,
                StockName = stockName,
                Unit = unit,
                OrderedQty = orderedQty
            };
            line.IssOrderLineId = issOrderLineId;
            line.StockMasterId = stockMasterId;
            return line;
        }

        public void RecordReturn(decimal returnedQty, ReturnReason? returnReason)
        {
            ReturnedQty = returnedQty;
            ReturnReason = returnReason;
        }

        public void ClearReturnData()
        {
            ReturnedQty  = null;
            ReturnReason = null;
        }

        /// <summary>
        /// Toplama miktarını set eder.
        /// Fazla toplama (DeliveredQty > OrderedQty) operasyonel olarak geçerlidir —
        /// örneğin koli tamamlama senaryolarında sipariş miktarı aşılabilir.
        /// Ancak herhangi bir fark (az veya fazla) varsa açıklama zorunludur.
        /// </summary>
        public void SetDeliveredQty(decimal qty, string? reason = null, string? note = null)
        {
            if (qty < 0)
                throw new DomainException("Toplama miktarı negatif olamaz.");

            DeliveredQty = qty;

            if (!string.IsNullOrWhiteSpace(note))
                Note = note;

            var finalReason = !string.IsNullOrWhiteSpace(reason) ? reason : DifferenceReason;

            // Sipariş miktarından farklıysa (az veya fazla) açıklama zorunlu.
            // Fazla toplama (örn. koli tamamlama) geçerlidir ama açıklaması olmalıdır.
            if (DeliveredQty != OrderedQty && string.IsNullOrWhiteSpace(finalReason))
                throw new DomainException("Toplanan miktar sipariş miktarından farklıysa açıklama zorunludur.");

            if (!string.IsNullOrWhiteSpace(reason))
                DifferenceReason = reason;
        }

        /// <summary>
        /// Toplama ilerlemesini taslak olarak kaydeder — gerekçe zorunluluğu uygulanmaz.
        /// Finalizasyon SetDeliveredQty ile yapılır.
        /// İsteğe bağlı fark nedeni de saklanır (boş geçilirse mevcut neden korunur).
        /// </summary>
        public void SavePickingProgress(decimal qty, string? differenceReason = null)
        {
            if (qty < 0)
                throw new DomainException("Toplama miktarı negatif olamaz.");
            DeliveredQty = qty;

            if (!string.IsNullOrWhiteSpace(differenceReason))
                DifferenceReason = differenceReason;
        }

        /// <summary>
        /// Picking verilerini sıfırlar. RevertToDraft senaryosunda çağrılır —
        /// bir sonraki picking turunda kirli veri kalmaması için.
        /// </summary>
        public void ResetPickingData()
        {
            DeliveredQty = 0;
            DifferenceReason = null;
        }

        /// <summary>
        /// Stok bilgilerini günceller (stok kartı değişikliği).
        /// </summary>
        public void UpdateStockInfo(string stockCode, string stockName, StockUnit unit, int? stockMasterId = null, bool updateStockMasterId = false)
        {
            StockCode = stockCode;
            StockName = stockName;
            Unit = unit;
            if (stockMasterId.HasValue)
                StockMasterId = stockMasterId.Value;
            else if (updateStockMasterId)
                StockMasterId = null;
        }

        /// <summary>
        /// Sipariş miktarını günceller (taslak aşamasında manuel düzenleme).
        /// </summary>
        public void UpdateOrderedQty(decimal qty)
        {
            OrderedQty = qty;
        }
    }
}
