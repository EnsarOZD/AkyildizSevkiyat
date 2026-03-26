using ClosedXML.Excel;
using MediatR;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Suppliers.Queries.GetSuppliersTemplate
{
    public record ExcelFileData(string FileName, string ContentType, byte[] Content);

    public record GetSuppliersTemplateQuery : IRequest<ExcelFileData>;

    public class GetSuppliersTemplateQueryHandler : IRequestHandler<GetSuppliersTemplateQuery, ExcelFileData>
    {
        public Task<ExcelFileData> Handle(GetSuppliersTemplateQuery request, CancellationToken cancellationToken)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Tedarikciler");

            // Headers
            ws.Cell(1, 1).Value = "Tedarikçi Adı";
            ws.Cell(1, 2).Value = "Kod";

            // Header style
            var header = ws.Range(1, 1, 1, 2);
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
            header.Style.Font.FontColor = XLColor.White;
            header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Example rows
            ws.Cell(2, 1).Value = "Örnek Tedarikçi A.Ş.";
            ws.Cell(2, 2).Value = "TED-001";

            ws.Cell(3, 1).Value = "Demo Lojistik Ltd.";
            ws.Cell(3, 2).Value = "";

            ws.Range(2, 1, 3, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#EFF6FF");

            // Notes
            ws.Cell(5, 1).Value = "Notlar:";
            ws.Cell(5, 1).Style.Font.Bold = true;
            ws.Cell(6, 1).Value = "• 'Tedarikçi Adı' zorunludur, 'Kod' isteğe bağlıdır.";
            ws.Cell(7, 1).Value = "• Aynı ada sahip mevcut tedarikçi varsa güncellenmez, yenisi eklenir.";
            ws.Cell(6, 1).Style.Font.Italic = true;
            ws.Cell(7, 1).Style.Font.Italic = true;

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return Task.FromResult(new ExcelFileData(
                "Tedarikci_Sablonu.xlsx",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                stream.ToArray()
            ));
        }
    }
}
