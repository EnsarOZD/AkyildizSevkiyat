using Akyildiz.Sevkiyat.Application.Interfaces;
using ExcelDataReader;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Projects.Commands.ImportProjectMappings
{
    public record ImportProjectMappingsCommand(Stream FileStream) : IRequest<ImportProjectMappingsResult>;

    public class ImportProjectMappingsResult
    {
        public int UpdatedCount { get; set; }
        public List<string> NotFoundProjects { get; set; } = new();
        public List<string> NotFoundZones { get; set; } = new();
        public List<string> Skipped { get; set; } = new();
    }

    public class ImportProjectMappingsCommandHandler : IRequestHandler<ImportProjectMappingsCommand, ImportProjectMappingsResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<ImportProjectMappingsCommandHandler> _logger;

        public ImportProjectMappingsCommandHandler(IApplicationDbContext context, ILogger<ImportProjectMappingsCommandHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ImportProjectMappingsResult> Handle(ImportProjectMappingsCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("--- Project Mapping Import Start ---");
            var result = new ImportProjectMappingsResult();

            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                using var reader = ExcelReaderFactory.CreateReader(request.FileStream);
                var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true }
                });

                if (dataSet.Tables.Count == 0) return result;

                var table = dataSet.Tables[0];
                
                // Get all zones for name lookup
                var zones = await _context.Zones.ToListAsync(cancellationToken);

                // Build a list of project codes from the excel file to fetch them in a single query
                var projectCodesToFetch = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
                foreach (DataRow row in table.Rows)
                {
                    var code = row[0]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(code))
                    {
                        projectCodesToFetch.Add(code);
                    }
                }

                var codesArray = projectCodesToFetch.ToArray();

                // Batch fetch projects from DB (Prevents N+1 query issue)
                var projectsByCode = await _context.Projects
                    .Where(p => codesArray.Contains(p.Code))
                    .ToDictionaryAsync(p => p.Code, p => p, System.StringComparer.OrdinalIgnoreCase, cancellationToken);

                foreach (DataRow row in table.Rows)
                {
                    var projectCode      = row[0]?.ToString()?.Trim();
                    var zoneName        = row[2]?.ToString()?.Trim();
                    var cariKodu        = row[3]?.ToString()?.Trim();
                    var teslimCariKodu  = table.Columns.Count > 4 ? row[4]?.ToString()?.Trim() : null;
                    var rawOrder        = table.Columns.Count > 5 ? row[5]?.ToString()?.Trim() : null;

                    if (string.IsNullOrEmpty(projectCode))
                    {
                        result.Skipped.Add("Proje kodu boş olan satır atlandı.");
                        continue;
                    }

                    if (!projectsByCode.TryGetValue(projectCode, out var project))
                    {
                        result.NotFoundProjects.Add(projectCode);
                        continue;
                    }

                    // Update Zone
                    if (!string.IsNullOrEmpty(zoneName))
                    {
                        var zone = zones.FirstOrDefault(z => z.Name.Equals(zoneName, System.StringComparison.OrdinalIgnoreCase));
                        if (zone != null)
                        {
                            project.ZoneId = zone.Id;
                        }
                        else
                        {
                            result.NotFoundZones.Add($"{zoneName} (Proje: {projectCode})");
                        }
                    }
                    else
                    {
                        project.ZoneId = null;
                    }

                    // Update Netsis Cari Kodu
                    project.NetsisCariKodu = string.IsNullOrEmpty(cariKodu) ? null : cariKodu;

                    // Update Netsis Teslim Cari Kodu (column E — optional)
                    if (teslimCariKodu is not null)
                        project.NetsisTeslimCariKodu = string.IsNullOrEmpty(teslimCariKodu) ? null : teslimCariKodu;

                    // Update Delivery Order
                    if (!string.IsNullOrEmpty(rawOrder) && int.TryParse(rawOrder, out int deliveryOrder))
                    {
                        project.DeliveryOrder = deliveryOrder;
                    }
                    else
                    {
                        project.DeliveryOrder = null;
                    }

                    result.UpdatedCount++;
                }

                if (result.UpdatedCount > 0)
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }

                _logger.LogInformation("--- Project Mapping Import End. Updated={Count} ---", result.UpdatedCount);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error during project mapping import");
                throw;
            }

            return result;
        }
    }
}
