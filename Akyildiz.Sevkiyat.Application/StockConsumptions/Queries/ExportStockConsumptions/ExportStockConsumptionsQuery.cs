using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using ClosedXML.Excel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.StockConsumptions.Queries.ExportStockConsumptions
{
    public record ExportStockConsumptionsData(string FileName, string ContentType, byte[] Content);

    public class ExportStockConsumptionsQuery : IRequest<ExportStockConsumptionsData>
    {
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        public StockConsumptionType? Type { get; set; }
        public string? Search { get; set; }
    }

    public class ExportStockConsumptionsQueryHandler : IRequestHandler<ExportStockConsumptionsQuery, ExportStockConsumptionsData>
    {
        private readonly IApplicationDbContext _context;

        public ExportStockConsumptionsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ExportStockConsumptionsData> Handle(ExportStockConsumptionsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.StockConsumptions.AsNoTracking();

            if (request.FromDate.HasValue)
                query = query.Where(x => x.Date >= request.FromDate.Value);
            if (request.ToDate.HasValue)
                query = query.Where(x => x.Date <= request.ToDate.Value);
            if (request.Type.HasValue)
                query = query.Where(x => x.Type == request.Type.Value);
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var s = request.Search.Trim();
                query = query.Where(x =>
                    x.StockCodeSnapshot.Contains(s) ||
                    x.StockNameSnapshot.Contains(s) ||
                    (x.RecipientName != null && x.RecipientName.Contains(s)));
            }

            var items = await query
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Stok Tüketim");

            // Headers
            ws.Cell(1, 1).Value = "Tarih";
            ws.Cell(1, 2).Value = "Tip";
            ws.Cell(1, 3).Value = "Stok Kodu";
            ws.Cell(1, 4).Value = "Stok Adı";
            ws.Cell(1, 5).Value = "Miktar";
            ws.Cell(1, 6).Value = "Birim";
            ws.Cell(1, 7).Value = "Sebep / Teslim Alan";
            ws.Cell(1, 8).Value = "Satış Fiyatı";
            ws.Cell(1, 9).Value = "Toplam Tutar";
            ws.Cell(1, 10).Value = "Not";
            ws.Cell(1, 11).Value = "Kaydeden";
            ws.Cell(1, 12).Value = "Kayıt Tarihi";

            var headerRange = ws.Range(1, 1, 1, 12);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            for (int i = 0; i < items.Count; i++)
            {
                var row = i + 2;
                var item = items[i];
                var typeLabel = item.Type == StockConsumptionType.Zai ? "Zai"
                              : item.Type == StockConsumptionType.DahiliKullanim ? "Dahili Kullanım"
                              : "Depo Satışı";
                var extraField = item.Type == StockConsumptionType.Zai ? item.Reason
                               : item.Type == StockConsumptionType.DahiliKullanim ? item.RecipientName
                               : null;

                ws.Cell(row, 1).Value = item.Date.ToDateTime(TimeOnly.MinValue);
                ws.Cell(row, 1).Style.NumberFormat.Format = "dd.MM.yyyy";
                ws.Cell(row, 2).Value = typeLabel;
                ws.Cell(row, 3).Value = item.StockCodeSnapshot;
                ws.Cell(row, 4).Value = item.StockNameSnapshot;
                ws.Cell(row, 5).Value = (double)item.Quantity;
                ws.Cell(row, 6).Value = item.UnitSnapshot.ToString();
                ws.Cell(row, 7).Value = extraField ?? "";
                ws.Cell(row, 8).Value = item.SalePrice.HasValue ? (double)item.SalePrice.Value : 0;
                ws.Cell(row, 9).Value = item.SalePrice.HasValue ? (double)(item.SalePrice.Value * item.Quantity) : 0;
                ws.Cell(row, 10).Value = item.Note ?? "";
                ws.Cell(row, 11).Value = item.CreatedBy ?? "";
                ws.Cell(row, 12).Value = item.CreatedAt.ToLocalTime();
                ws.Cell(row, 12).Style.NumberFormat.Format = "dd.MM.yyyy HH:mm";

                // Satır rengi tipe göre
                var rowRange = ws.Range(row, 1, row, 12);
                if (item.Type == StockConsumptionType.Zai)
                    rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF3CD");
                else if (item.Type == StockConsumptionType.DepoSatisi)
                    rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#D1E7DD");
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var bytes = stream.ToArray();

            var dateRange = request.FromDate.HasValue || request.ToDate.HasValue
                ? $"_{request.FromDate?.ToString("yyyyMMdd") ?? ""}_{request.ToDate?.ToString("yyyyMMdd") ?? ""}"
                : "";

            return new ExportStockConsumptionsData(
                $"StokTuketim{dateRange}.xlsx",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                bytes);
        }
    }
}
