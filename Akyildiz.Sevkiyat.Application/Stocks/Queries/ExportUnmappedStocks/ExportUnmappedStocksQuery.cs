using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using ClosedXML.Excel;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Stocks.Queries.ExportUnmappedStocks
{
    public record ExportUnmappedData(string FileName, string ContentType, byte[] Content);

    public record ExportUnmappedStocksQuery : IRequest<ExportUnmappedData>;

    public class ExportUnmappedStocksQueryHandler : IRequestHandler<ExportUnmappedStocksQuery, ExportUnmappedData>
    {
        private readonly IApplicationDbContext _context;

        public ExportUnmappedStocksQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ExportUnmappedData> Handle(ExportUnmappedStocksQuery request, CancellationToken cancellationToken)
        {
            // Fetch unmapped items (MatchStatus.Pending)
            var unmapped = await _context.StockMappings
                .Where(m => m.MatchStatus == MatchStatus.Unmapped)
                .OrderBy(m => m.ExternalStockCode)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Eslesmeyen Stoklar");

            // Headers
            worksheet.Cell(1, 1).Value = "External Code (ISS-IP)";
            worksheet.Cell(1, 2).Value = "External Name";
            worksheet.Cell(1, 3).Value = "Local Stock Code (Eslestirilecek)";
            
            // Helpful note column (optional, not read back)
            worksheet.Cell(1, 4).Value = "Not";

            // Data
            for (int i = 0; i < unmapped.Count; i++)
            {
                var row = i + 2;
                var item = unmapped[i];

                worksheet.Cell(row, 1).Value = item.ExternalStockCode;
                worksheet.Cell(row, 2).Value = item.ExternalStockName;
                // Column 3 is empty for user to fill
                worksheet.Cell(row, 3).Value = ""; 
                worksheet.Cell(row, 4).Value = "Lutfen C sutununa yerel stok kodunu giriniz.";
            }

            // Styling
            var headerRange = worksheet.Range(1, 1, 1, 4);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.Yellow; // Attention color
            worksheet.Columns().AdjustToContents();

            // Protect columns A and B to prevent modification? Maybe too restrictive, but good practice.
            // keeping it simple for now.

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return new ExportUnmappedData("EslestirilecekStoklar.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", content);
        }
    }
}
