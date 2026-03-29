using Akyildiz.Sevkiyat.Application.Interfaces;
using ClosedXML.Excel;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Projects.Queries.ExportProjectMappings
{
    public record ExportProjectMappingsData(string FileName, string ContentType, byte[] Content);

    public record ExportProjectMappingsQuery : IRequest<ExportProjectMappingsData>;

    public class ExportProjectMappingsQueryHandler : IRequestHandler<ExportProjectMappingsQuery, ExportProjectMappingsData>
    {
        private readonly IApplicationDbContext _context;

        public ExportProjectMappingsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ExportProjectMappingsData> Handle(ExportProjectMappingsQuery request, CancellationToken cancellationToken)
        {
            var projects = await _context.Projects
                .Include(p => p.Zone)
                .OrderBy(p => p.Code)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Proje Bolge Eslesmeleri");

            // Headers
            worksheet.Cell(1, 1).Value = "Proje Kodu";
            worksheet.Cell(1, 2).Value = "Proje Adı";
            worksheet.Cell(1, 3).Value = "Bölge Adı";
            worksheet.Cell(1, 4).Value = "Netsis Cari Kodu";
            worksheet.Cell(1, 5).Value = "Teslimat Sırası";

            // Data
            for (int i = 0; i < projects.Count; i++)
            {
                var row = i + 2;
                var project = projects[i];

                worksheet.Cell(row, 1).Value = project.Code;
                worksheet.Cell(row, 2).Value = project.Name;
                worksheet.Cell(row, 3).Value = project.Zone?.Name ?? "";
                worksheet.Cell(row, 4).Value = project.NetsisCariKodu ?? "";
                worksheet.Cell(row, 5).Value = project.DeliveryOrder;
            }

            // Styling
            var headerRange = worksheet.Range(1, 1, 1, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return new ExportProjectMappingsData(
                "ProjeBolgeEslesmeleri.xlsx", 
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                content);
        }
    }
}
