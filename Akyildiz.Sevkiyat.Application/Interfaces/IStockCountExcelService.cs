using System.Collections.Generic;

namespace Akyildiz.Sevkiyat.Application.Interfaces
{
    public interface IStockCountExcelService
    {
        byte[] GenerateTemplate(IEnumerable<Models.StockCountTemplateRowDto> rows);
        IEnumerable<Models.ParsedExcelRowDto> ParseImportData(byte[] fileContent);
    }
}

namespace Akyildiz.Sevkiyat.Application.Interfaces.Models
{
    public class StockCountTemplateRowDto
    {
        public int LineId { get; set; }
        public string StockCode { get; set; } = null!;
        public string StockName { get; set; } = null!;
        public decimal ExpectedQty { get; set; }
        public decimal? ActualQty { get; set; }
    }

    public class ParsedExcelRowDto
    {
        public int? LineId { get; set; }
        public decimal? ActualQty { get; set; }
        public int RowNumber { get; set; }
    }
}
