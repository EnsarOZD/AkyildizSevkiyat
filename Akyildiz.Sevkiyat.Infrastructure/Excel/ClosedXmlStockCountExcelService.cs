using System.Collections.Generic;
using System.IO;
using System.Linq;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces.Models;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using ClosedXML.Excel;

namespace Akyildiz.Sevkiyat.Infrastructure.Excel
{
    public class ClosedXmlStockCountExcelService : IStockCountExcelService
    {
        public byte[] GenerateTemplate(IEnumerable<StockCountTemplateRowDto> rows)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Sayım Şablonu");

            // Headers
            worksheet.Cell(1, 1).Value = "LineId";
            worksheet.Cell(1, 2).Value = "StockCode";
            worksheet.Cell(1, 3).Value = "StockName";
            worksheet.Cell(1, 4).Value = "ExpectedQty";
            worksheet.Cell(1, 5).Value = "ActualQty";

            // Format headers
            var headerRange = worksheet.Range("A1:E1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            int row = 2;
            foreach (var item in rows)
            {
                worksheet.Cell(row, 1).Value = item.LineId;
                worksheet.Cell(row, 2).Value = item.StockCode;
                worksheet.Cell(row, 3).Value = item.StockName;
                worksheet.Cell(row, 4).Value = item.ExpectedQty;
                
                if (item.ActualQty.HasValue)
                {
                    worksheet.Cell(row, 5).Value = item.ActualQty.Value;
                }
                else
                {
                    worksheet.Cell(row, 5).Value = "";
                }
                
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public IEnumerable<ParsedExcelRowDto> ParseImportData(byte[] fileContent)
        {
            if (fileContent == null || fileContent.Length == 0)
                throw new DomainException("Yüklenen dosya boş olamaz.");

            using var stream = new MemoryStream(fileContent);
            XLWorkbook workbook;
            try
            {
                workbook = new XLWorkbook(stream);
            }
            catch
            {
                throw new DomainException("Geçersiz Excel dosyası formatı.");
            }

            using var wb = workbook;
            var worksheet = wb.Worksheets.FirstOrDefault();
            if (worksheet == null)
                throw new DomainException("Excel dosyası sayfa içermiyor.");

            // Find headers
            var firstRow = worksheet.FirstRowUsed();
            if (firstRow == null)
                throw new DomainException("Excel dosyası boş.");

            int? lineIdCol = null;
            int? actualQtyCol = null;

            foreach (var cell in firstRow.CellsUsed())
            {
                var val = cell.GetValue<string>()?.Trim();
                if (val == "LineId") lineIdCol = cell.Address.ColumnNumber;
                if (val == "ActualQty") actualQtyCol = cell.Address.ColumnNumber;
            }

            if (lineIdCol == null)
                throw new DomainException("Gerekli kolon bulunamadı: 'LineId'. Lütfen şablonu değiştirmeden kullanın.");
            if (actualQtyCol == null)
                throw new DomainException("Gerekli kolon bulunamadı: 'ActualQty'. Lütfen şablonu değiştirmeden kullanın.");

            var results = new List<ParsedExcelRowDto>();
            var rows = worksheet.RowsUsed().Skip(1); // Skip header

            foreach (var wbRow in rows)
            {
                var lineIdCell = wbRow.Cell(lineIdCol.Value);
                if (lineIdCell.IsEmpty())
                    throw new DomainException($"Satır {wbRow.RowNumber()}: 'LineId' boş olamaz.");

                if (!lineIdCell.TryGetValue<int>(out var lineId))
                    throw new DomainException($"Satır {wbRow.RowNumber()}: 'LineId' ({lineIdCell.GetString()}) geçerli bir sayı değil.");

                var actualQtyCell = wbRow.Cell(actualQtyCol.Value);
                decimal? actualQty = null;

                if (!actualQtyCell.IsEmpty())
                {
                    if (!actualQtyCell.TryGetValue<decimal>(out var qty))
                        throw new DomainException($"Satır {wbRow.RowNumber()} (LineId: {lineId}): 'ActualQty' ({actualQtyCell.GetString()}) geçerli bir sayı değil.");
                    
                    if (qty < 0)
                        throw new DomainException($"Satır {wbRow.RowNumber()} (LineId: {lineId}): Girilen miktar ({qty}) sıfırdan küçük olamaz.");

                    actualQty = qty;
                }

                results.Add(new ParsedExcelRowDto
                {
                    LineId = lineId,
                    ActualQty = actualQty,
                    RowNumber = wbRow.RowNumber()
                });
            }

            // Check duplicates
            var duplicates = results.GroupBy(x => x.LineId).Where(g => g.Count() > 1).ToList();
            if (duplicates.Any())
            {
                var dupIds = string.Join(", ", duplicates.Select(x => x.Key));
                throw new DomainException($"Excel dosyasında mükerrer LineId'ler bulundu: {dupIds}");
            }

            return results;
        }
    }
}
