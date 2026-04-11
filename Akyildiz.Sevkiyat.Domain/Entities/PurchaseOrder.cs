using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    public class PurchaseOrder : AuditableEntity
    {
        public Guid Id { get; set; }

        public string OrderNumber { get; set; } = string.Empty; // New 15 chars strict format

        // Foreign Key to new Supplier Entity
        public Guid SupplierId { get; set; } 
        public Supplier Supplier { get; set; } = null!;
        
        public string SupplierNameSnapshot { get; set; } = string.Empty;
        public DateOnly OrderDate { get; set; }
        public DateOnly? ExpectedDeliveryDate { get; set; }

        public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

        public string? ExternalRef { get; set; }         // Netsis PO No
        public DateTime? NetsisTransferredAt { get; set; } // İdempotency: dolu ise yeniden aktarma
        public string? Note { get; set; }

        public ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();

        [NotMapped]
        public bool IsEditable => Status == PurchaseOrderStatus.Draft;
    }

    public class PurchaseOrderLine : AuditableEntity
    {
        public Guid Id { get; set; }

        public Guid PurchaseOrderId { get; set; }
        public PurchaseOrder PurchaseOrder { get; set; } = null!;

        public int StockMasterId { get; set; }
        public StockMaster StockMaster { get; set; } = null!;

        public decimal OrderedQty { get; set; }
        public StockUnit Unit { get; set; } = StockUnit.Adet;
        [Column(TypeName = "decimal(18,4)")]
        public decimal? UnitPrice { get; set; }

        public string? Note { get; set; }
    }
}
