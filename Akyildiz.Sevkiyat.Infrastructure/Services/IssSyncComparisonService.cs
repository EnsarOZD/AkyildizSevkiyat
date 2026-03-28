using System.Text.Json;
using Akyildiz.Sevkiyat.Application.External.IssIp.Dtos;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.RouteOptimization.Dtos;
using Akyildiz.Sevkiyat.Application.RouteOptimization.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Akyildiz.Sevkiyat.Infrastructure.Services
{
    public class IssSyncComparisonService : IIssSyncComparisonService
    {
        private readonly IISSIpClient _issClient;
        private readonly IApplicationDbContext _context;
        private readonly ILogger<IssSyncComparisonService> _logger;

        public IssSyncComparisonService(
            IISSIpClient issClient,
            IApplicationDbContext context,
            ILogger<IssSyncComparisonService> logger)
        {
            _issClient = issClient;
            _context = context;
            _logger = logger;
        }

        public async Task<List<ProjectSyncComparisonDto>> CompareWithIssAsync(
            List<string> projectCodes,
            CancellationToken cancellationToken = default)
        {
            var projects = await _context.Projects
                .Where(p => projectCodes.Contains(p.Code))
                .ToListAsync(cancellationToken);

            var results = new List<ProjectSyncComparisonDto>();
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (var project in projects)
            {
                try
                {
                    var envelope = await _issClient.GetProjeAsync(project.Code, cancellationToken);
                    var root = GetRefinedRoot(envelope.Root);

                    IssProjeDto? dto = null;

                    if (root.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        if (TryGetProperty(root, out var tableProp, "Table", "Proje", "Data", "Result"))
                        {
                            var list = JsonSerializer.Deserialize<List<IssProjeDto>>(tableProp.GetRawText(), opts);
                            dto = list?.FirstOrDefault();
                        }
                        else
                        {
                            try { dto = JsonSerializer.Deserialize<IssProjeDto>(root.GetRawText(), opts); } catch { }
                        }
                    }
                    else if (root.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        var list = JsonSerializer.Deserialize<List<IssProjeDto>>(root.GetRawText(), opts);
                        dto = list?.FirstOrDefault();
                    }

                    if (dto == null)
                    {
                        _logger.LogWarning("ISS comparison: proje bulunamadı {ProjectCode}", project.Code);
                        continue;
                    }

                    var nameChanged   = !string.IsNullOrWhiteSpace(dto.ProjeAdi) &&
                                        !string.Equals(project.Name, dto.ProjeAdi, StringComparison.OrdinalIgnoreCase);
                    var addressChanged = !string.IsNullOrWhiteSpace(dto.MalzemeTeslimAdresi) &&
                                        !string.Equals(project.Address, dto.MalzemeTeslimAdresi, StringComparison.OrdinalIgnoreCase);

                    results.Add(new ProjectSyncComparisonDto(
                        ProjectCode:    project.Code,
                        ProjectName:    project.Name,
                        CurrentName:    project.Name,
                        IssName:        dto.ProjeAdi,
                        NameChanged:    nameChanged,
                        CurrentAddress: project.Address,
                        IssAddress:     dto.MalzemeTeslimAdresi,
                        AddressChanged: addressChanged
                    ));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ISS comparison hatası: {ProjectCode}", project.Code);
                }
            }

            return results;
        }

        private System.Text.Json.JsonElement GetRefinedRoot(System.Text.Json.JsonElement root)
        {
            if (root.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                if (TryGetProperty(root, out var resProp, "result", "Result") &&
                    resProp.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    try
                    {
                        var inner = resProp.GetString();
                        if (!string.IsNullOrWhiteSpace(inner))
                        {
                            var doc = System.Text.Json.JsonDocument.Parse(inner);
                            return doc.RootElement.Clone();
                        }
                    }
                    catch { }
                }
            }
            return root;
        }

        private bool TryGetProperty(System.Text.Json.JsonElement element, out System.Text.Json.JsonElement value, params string[] names)
        {
            value = default;
            foreach (var name in names)
                if (element.TryGetProperty(name, out value)) return true;
            return false;
        }
    }
}
