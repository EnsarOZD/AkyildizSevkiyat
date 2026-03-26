using Akyildiz.Sevkiyat.Application.Interfaces;
using ClosedXML.Excel;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Stocks.Queries.ExportStocks
{
    public record ExportStocksData(string FileName, string ContentType, byte[] Content);

    public record ExportStocksQuery : IRequest<ExportStocksData>;

    public class ExportStocksQueryHandler : IRequestHandler<ExportStocksQuery, ExportStocksData>
    {
        private readonly IApplicationDbContext _context;

        public ExportStocksQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ExportStocksData> Handle(ExportStocksQuery request, CancellationToken cancellationToken)
        {
            var stocks = await _context.StockMasters
                .OrderBy(s => s.StockCode)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Stok Listesi");

            // Headers
            worksheet.Cell(1, 1).Value = "Stok Kodu";
            worksheet.Cell(1, 2).Value = "Stok Adı";
            worksheet.Cell(1, 3).Value = "Birim";
            worksheet.Cell(1, 4).Value = "Birim Fiyat";
            worksheet.Cell(1, 5).Value = "KDV Oranı";
            worksheet.Cell(1, 6).Value = "Toplama Tipi";
            worksheet.Cell(1, 7).Value = "Durum";
            worksheet.Cell(1, 8).Value = "Kategori";

            // Data
            for (int i = 0; i < stocks.Count; i++)
            {
                try
                {
                    var row = i + 2;
                    var stock = stocks[i];

                    worksheet.Cell(row, 1).Value = stock.StockCode;
                    worksheet.Cell(row, 2).Value = stock.StockName;
                    worksheet.Cell(row, 3).Value = stock.Unit.ToString();
                    worksheet.Cell(row, 4).Value = (double)stock.UnitPrice;
                    worksheet.Cell(row, 5).Value = (int)stock.TaxRate;
                    worksheet.Cell(row, 6).Value = stock.PickingType.ToString();
                    worksheet.Cell(row, 7).Value = stock.IsActive ? "Aktif" : "Pasif";
                    worksheet.Cell(row, 8).Value = stock.Category.ToString();
                }
                catch (System.Exception ex)
                {
                    System.Console.WriteLine($"Export row error: {ex.Message}");
                }
            }

            // Styling
            var headerRange = worksheet.Range(1, 1, 1, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return new ExportStocksData("StokListesi.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", content);
        }
    }
}
