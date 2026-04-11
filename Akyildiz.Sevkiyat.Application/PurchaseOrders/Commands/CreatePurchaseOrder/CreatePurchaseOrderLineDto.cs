namespace Akyildiz.Sevkiyat.Application.PurchaseOrders.Commands.CreatePurchaseOrder
{
    public class CreatePurchaseOrderLineDto
    {
        public int StockMasterId { get; set; }
        public decimal OrderedQty { get; set; }
        public decimal? UnitPrice { get; set; }
        public string? Note { get; set; }
    }
}
