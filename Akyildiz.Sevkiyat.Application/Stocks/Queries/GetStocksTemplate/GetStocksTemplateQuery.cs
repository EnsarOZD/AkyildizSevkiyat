using ClosedXML.Excel;
using MediatR;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Stocks.Queries.GetStocksTemplate
{
    public record ExcelFileData(string FileName, string ContentType, byte[] Content);

    public record GetStocksTemplateQuery : IRequest<ExcelFileData>;

    public class GetStocksTemplateQueryHandler : IRequestHandler<GetStocksTemplateQuery, ExcelFileData>
    {
        public Task<ExcelFileData> Handle(GetStocksTemplateQuery request, CancellationToken cancellationToken)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Stoklar");

            // Headers
            ws.Cell(1, 1).Value = "Kod";
            ws.Cell(1, 2).Value = "Ad";
            ws.Cell(1, 3).Value = "Birim";
            ws.Cell(1, 4).Value = "Fiyat";
            ws.Cell(1, 5).Value = "KDV";
            ws.Cell(1, 6).Value = "Tip";
            ws.Cell(1, 7).Value = "Durum";
            ws.Cell(1, 8).Value = "Kategori";

            // Header style
            var header = ws.Range(1, 1, 1, 8);
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
            header.Style.Font.FontColor = XLColor.White;
            header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Example rows
            ws.Cell(2, 1).Value = "STK-001";
            ws.Cell(2, 2).Value = "Örnek Stok Adı";
            ws.Cell(2, 3).Value = "Adet";
            ws.Cell(2, 4).Value = "10.50";
            ws.Cell(2, 5).Value = "20";
            ws.Cell(2, 6).Value = "Micro";
            ws.Cell(2, 7).Value = "Aktif";
            ws.Cell(2, 8).Value = "Sarf";

            ws.Cell(3, 1).Value = "STK-002";
            ws.Cell(3, 2).Value = "İkinci Stok Örneği";
            ws.Cell(3, 3).Value = "Kg";
            ws.Cell(3, 4).Value = "25.00";
            ws.Cell(3, 5).Value = "10";
            ws.Cell(3, 6).Value = "Macro";
            ws.Cell(3, 7).Value = "Aktif";
            ws.Cell(3, 8).Value = "Gida";

            // Example row style (light)
            ws.Range(2, 1, 3, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#EFF6FF");

            // Notes sheet
            var notes = workbook.Worksheets.Add("Açıklamalar");
            notes.Cell(1, 1).Value = "Sütun";
            notes.Cell(1, 2).Value = "Zorunlu";
            notes.Cell(1, 3).Value = "Kabul Edilen Değerler";
            notes.Cell(1, 1).Style.Font.Bold = true;
            notes.Cell(1, 2).Style.Font.Bold = true;
            notes.Cell(1, 3).Style.Font.Bold = true;

            var noteData = new[]
            {
                ("Kod",      "Evet", "Herhangi bir metin (boş olamaz)"),
                ("Ad",       "Evet", "Herhangi bir metin (boş olamaz)"),
                ("Birim",    "Hayır","Adet, Kg, Paket, Koli, Litre, Metre, Metrekare, Set, Teneke, Diger"),
                ("Fiyat",    "Hayır","Ondalık sayı (örn: 10.50 veya 10,50)"),
                ("KDV",      "Hayır","0, 1, 10, 20"),
                ("Tip",      "Hayır","Micro, Macro (boş bırakılırsa Unassigned)"),
                ("Durum",    "Hayır","Aktif, Pasif (boş = Aktif)"),
                ("Kategori", "Hayır","Gida, Sarf, Kiyafet, Kirtasiye, Diger (boş = Tanımsız)"),
            };

            for (int i = 0; i < noteData.Length; i++)
            {
                notes.Cell(i + 2, 1).Value = noteData[i].Item1;
                notes.Cell(i + 2, 2).Value = noteData[i].Item2;
                notes.Cell(i + 2, 3).Value = noteData[i].Item3;
            }
            notes.Columns().AdjustToContents();

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return Task.FromResult(new ExcelFileData(
                "Stok_Sablonu.xlsx",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                stream.ToArray()
            ));
        }
    }
}
