namespace Akyildiz.Sevkiyat.Application.Reports.Queries.GetZoneMaterialReport
{
    public class ZoneMaterialRowDto
    {
        public int ZoneId { get; set; }
        public string ZoneName { get; set; } = string.Empty;
        public int? StockMasterId { get; set; }
        public string StockCode { get; set; } = string.Empty;
        public string StockName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal TotalQty { get; set; }
        public int ShipmentCount { get; set; }
    }
}
