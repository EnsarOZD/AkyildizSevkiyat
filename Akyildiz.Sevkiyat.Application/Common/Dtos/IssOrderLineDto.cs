namespace Akyildiz.Sevkiyat.Application.Common.Dtos
{
    public record IssOrderLineDto(
        int Id,
        int LineNumber,
        string StockCode,
        string StockName,
        string Unit,
        decimal OrderedQty
    );
}
