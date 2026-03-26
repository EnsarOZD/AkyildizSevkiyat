using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    public class GoodsReceipt : AuditableEntity
    {
        public Guid Id { get; set; }

        public Guid? PurchaseOrderId { get; set; }
        public PurchaseOrder? PurchaseOrder { get; set; }

        public Guid SupplierId { get; set; }
        public string SupplierNameSnapshot { get; set; } = string.Empty;

        public DateOnly ReceiptDate { get; set; }
        public string WaybillNo { get; set; } = string.Empty;
        public DateOnly WaybillDate { get; set; }

        public GoodsReceiptStatus Status { get; set; } = GoodsReceiptStatus.Draft;

        public string? ExternalRef { get; set; } // Netsis Waybill Ref (future)
        public string? Note { get; set; }

        public ICollection<GoodsReceiptLine> Lines { get; set; } = new List<GoodsReceiptLine>();

        [NotMapped]
        public bool IsEditable => Status == GoodsReceiptStatus.Draft;
    }

    public class GoodsReceiptLine : AuditableEntity
    {
        public Guid Id { get; set; }

        public Guid GoodsReceiptId { get; set; }
        public GoodsReceipt GoodsReceipt { get; set; } = null!;

        public Guid? PurchaseOrderLineId { get; set; }
        public PurchaseOrderLine? PurchaseOrderLine { get; set; }

        public int StockMasterId { get; set; }
        public StockMaster StockMaster { get; set; } = null!;

        public string? StockNameSnapshot { get; set; }
        public StockUnit UnitSnapshot { get; set; } = StockUnit.Adet;

        public decimal OrderedQty { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal? AcceptedQty { get; set; }
        public decimal? RejectedQty { get; set; } // Optional
        public string? RejectReason { get; set; }
        
        public string? Note { get; set; }
    }
}
