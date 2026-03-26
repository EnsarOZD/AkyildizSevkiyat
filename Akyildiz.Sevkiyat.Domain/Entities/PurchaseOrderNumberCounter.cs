namespace Akyildiz.Sevkiyat.Domain.Entities
{
    public class PurchaseOrderNumberCounter
    {
        public int Id { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int LastValue { get; set; }
    }
}
