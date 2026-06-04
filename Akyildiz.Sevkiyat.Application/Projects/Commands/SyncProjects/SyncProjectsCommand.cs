using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Akyildiz.Sevkiyat.Application.External.IssIp.Dtos;
using Microsoft.Extensions.Logging;

namespace Akyildiz.Sevkiyat.Application.Projects.Commands.SyncProjects
{
    public class SyncProjectsCommand : IRequest<SyncProjectsResult>
    {
        public bool ForceAll { get; set; } = false;
    }

    public record SyncProjectsResult(int SyncedCount, List<ProjectAddressChangeDto> AddressChanges);

    public record ProjectAddressChangeDto(
        int ProjectId,
        string ProjectCode,
        string ProjectName,
        string? OldAddress,
        string? NewAddress);

    public class SyncProjectsCommandHandler : IRequestHandler<SyncProjectsCommand, SyncProjectsResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly IISSIpClient _issClient;
        private readonly ILogger<SyncProjectsCommandHandler> _logger;

        public SyncProjectsCommandHandler(IApplicationDbContext context, IISSIpClient issClient, ILogger<SyncProjectsCommandHandler> logger)
        {
            _context = context;
            _issClient = issClient;
            _logger = logger;
        }

        public async Task<SyncProjectsResult> Handle(SyncProjectsCommand request, CancellationToken cancellationToken)
        {
            var query = _context.Projects.AsQueryable();

            if (!request.ForceAll)
            {
                var cutoff = DateTime.UtcNow.AddHours(-24);
                query = query.Where(p => p.LastSyncedAt == null || p.LastSyncedAt < cutoff);
            }

            var projectsToSync = await query.ToListAsync(cancellationToken);
            _logger.LogInformation("SyncProjects started: {Count} projects, ForceAll={ForceAll}", projectsToSync.Count, request.ForceAll);

            if (projectsToSync.Count == 0)
                return new SyncProjectsResult(0, new());

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Paralel HTTP çağrıları — max 5 eş zamanlı (ISS-IP rate limit gözetilerek)
            var semaphore = new SemaphoreSlim(5, 5);
            var results = new System.Collections.Concurrent.ConcurrentDictionary<string, IssProjeDto?>();

            var fetchTasks = projectsToSync.Select(async project =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var envelope = await _issClient.GetProjeAsync(project.Code, cancellationToken);
                    var root = GetRefinedRoot(envelope.Root);

                    IssProjeDto? dto = null;

                    if (root.ValueKind == JsonValueKind.Object)
                    {
                        if (TryGetProperty(root, out var tableProj, "Table", "Proje", "Data", "Result"))
                            dto = JsonSerializer.Deserialize<List<IssProjeDto>>(tableProj.GetRawText(), opts)?.FirstOrDefault();
                        else
                        {
                            try { dto = JsonSerializer.Deserialize<IssProjeDto>(root.GetRawText(), opts); } catch { }
                        }
                    }
                    else if (root.ValueKind == JsonValueKind.Array)
                    {
                        dto = JsonSerializer.Deserialize<List<IssProjeDto>>(root.GetRawText(), opts)?.FirstOrDefault();
                    }

                    results[project.Code ?? ""] = dto;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SyncProjects HTTP error for project {ProjectCode}", project.Code);
                    results[project.Code ?? ""] = null;
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(fetchTasks);

            // EF Core context'i tek thread'de güncelle
            int syncedCount = 0;
            var addressChanges = new List<ProjectAddressChangeDto>();
            var now = DateTime.UtcNow;
            foreach (var project in projectsToSync)
            {
                var dto = results.GetValueOrDefault(project.Code ?? "");
                if (dto != null)
                {
                    project.Name = dto.ProjeAdi ?? project.Name;
                    project.InstitutionCode = dto.KurumKodu ?? project.InstitutionCode;

                    // Adres değişikliği tespiti — yeni adres geldiyse ve eskiyle anlamlı farklıysa kaydet
                    var newAddress = dto.MalzemeTeslimAdresi;
                    if (!string.IsNullOrWhiteSpace(newAddress) && !AddressEquals(project.Address, newAddress))
                    {
                        var change = new ProjectAddressChange
                        {
                            ProjectId   = project.Id,
                            ProjectCode = project.Code ?? string.Empty,
                            ProjectName = project.Name,
                            OldAddress  = project.Address,
                            NewAddress  = newAddress,
                            ChangedAt   = now
                        };
                        _context.ProjectAddressChanges.Add(change);
                        addressChanges.Add(new ProjectAddressChangeDto(
                            project.Id, project.Code ?? string.Empty, project.Name, project.Address, newAddress));

                        project.Address = newAddress;

                        // Adres değişti → mevcut koordinat artık şüpheli olabilir; yeniden kontrol işaretle.
                        if (project.Latitude.HasValue && project.Longitude.HasValue)
                            project.LocationNeedsRecheck = true;
                    }
                    else if (!string.IsNullOrWhiteSpace(newAddress))
                    {
                        project.Address = newAddress;
                    }

                    syncedCount++;
                }
                project.LastSyncedAt = now;
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("SyncProjects completed: {SyncedCount}/{TotalCount} projects updated, {ChangeCount} address changes",
                syncedCount, projectsToSync.Count, addressChanges.Count);
            return new SyncProjectsResult(syncedCount, addressChanges);
        }

        private static readonly System.Globalization.CultureInfo TrCulture = new("tr-TR");
        /// <summary>Adresleri boşluk/büyük-küçük harf duyarsız karşılaştırır (gürültüyü azaltmak için).</summary>
        private static bool AddressEquals(string? a, string? b)
        {
            static string Norm(string? s) =>
                System.Text.RegularExpressions.Regex.Replace((s ?? string.Empty).Trim(), @"\s+", " ").ToLower(TrCulture);
            return Norm(a) == Norm(b);
        }

        // --- Helper Methods (Duplicated from ImportIssOrdersCommand for speed, should be refactored to Shared/Utils later) ---
        private JsonElement GetRefinedRoot(JsonElement root)
        {
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (TryGetProperty(root, out var resProp, "result", "Result") && resProp.ValueKind == JsonValueKind.String)
                {
                    try
                    {
                        var innerJson = resProp.GetString();
                        if (!string.IsNullOrWhiteSpace(innerJson))
                        {
                            var doc = JsonDocument.Parse(innerJson);
                            return doc.RootElement.Clone(); // Clone to be safe
                        }
                    }
                    catch { }
                }
            }
            return root;
        }

        private bool TryGetProperty(JsonElement element, out JsonElement value, params string[] propertyNames)
        {
            value = default;
            foreach (var name in propertyNames)
            {
                if (element.TryGetProperty(name, out value)) return true;
            }
            return false;
        }
    }
}
