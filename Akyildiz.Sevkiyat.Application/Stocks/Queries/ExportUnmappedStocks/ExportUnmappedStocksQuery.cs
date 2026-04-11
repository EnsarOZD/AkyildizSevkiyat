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
            // Unmapped + mevcut NetsisStockCode için LocalStock join
            var unmapped = await _context.StockMappings
                .Where(m => m.MatchStatus == MatchStatus.Unmapped)
                .OrderBy(m => m.ExternalStockCode)
                .Select(m => new
                {
                    m.ExternalStockCode,
                    m.ExternalStockName,
                    LocalStockCode      = m.LocalStock != null ? m.LocalStock.StockCode    : null,
                    NetsisStockCode     = m.LocalStock != null ? m.LocalStock.NetsisStockCode : null,
                })
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Eslesmeyen Stoklar");

            // Headers
            worksheet.Cell(1, 1).Value = "ISS Kodu (A - degistirme)";
            worksheet.Cell(1, 2).Value = "ISS Adi (B - degistirme)";
            worksheet.Cell(1, 3).Value = "Yerel Stok Kodu (C - doldur)";
            worksheet.Cell(1, 4).Value = "Netsis Stok Kodu (D - doldur)";

            // Data
            for (int i = 0; i < unmapped.Count; i++)
            {
                var row = i + 2;
                var item = unmapped[i];

                worksheet.Cell(row, 1).Value = item.ExternalStockCode;
                worksheet.Cell(row, 2).Value = item.ExternalStockName;
                worksheet.Cell(row, 3).Value = item.LocalStockCode    ?? "";
                worksheet.Cell(row, 4).Value = item.NetsisStockCode   ?? "";
            }

            // Styling — başlık satırı
            var headerRange = worksheet.Range(1, 1, 1, 4);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.Yellow;

            // A ve B sütunları salt-okunur görünümü (gri arka plan)
            var readonlyRange = worksheet.Range(2, 1, unmapped.Count + 1, 2);
            readonlyRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            // D sütununu turuncu — zorunlu alan
            worksheet.Cell(1, 4).Style.Fill.BackgroundColor = XLColor.Orange;

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
