using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Akyildiz.Sevkiyat.Application.External.IssIp.Dtos;
using Microsoft.Extensions.Logging;

namespace Akyildiz.Sevkiyat.Application.Projects.Commands.SyncProjects
{
    public class SyncProjectsCommand : IRequest<int>
    {
        public bool ForceAll { get; set; } = false;
    }

    public class SyncProjectsCommandHandler : IRequestHandler<SyncProjectsCommand, int>
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

        public async Task<int> Handle(SyncProjectsCommand request, CancellationToken cancellationToken)
        {
            // Fetch projects that need sync:
            // 1. Never synced (LastSyncedAt == null)
            // 2. Synced > 24 hours ago (if not forced)
            // 3. Or All if ForceAll is true
            
            var query = _context.Projects.AsQueryable();

            if (!request.ForceAll)
            {
                var cutoff = DateTime.UtcNow.AddHours(-24);
                query = query.Where(p => p.LastSyncedAt == null || p.LastSyncedAt < cutoff);
            }

            var projectsToSync = await query.ToListAsync(cancellationToken);
            _logger.LogInformation("Starting Sync. Found {ProjectCount} projects to sync. ForceAll={ForceAll}", projectsToSync.Count, request.ForceAll);

            int syncedCount = 0;

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (var project in projectsToSync)
            {
                try
                {
                    // Use Code as ProjeKodu
                    var envelope = await _issClient.GetProjeAsync(project.Code, cancellationToken);
                    var root = GetRefinedRoot(envelope.Root); 
                    
                    // DEBUG LOG Response
                    string rawJson = root.ValueKind == JsonValueKind.Undefined ? "Undefined" : root.GetRawText();
                    _logger.LogInformation("Project {ProjectCode} API Response: {RawJsonResponse}", project.Code, rawJson);

                    IssProjeDto? dto = null;

                    if (root.ValueKind == JsonValueKind.Object)
                    {
                        if (TryGetProperty(root, out var tableProj, "Table", "Proje", "Data", "Result"))
                        {
                            var list = JsonSerializer.Deserialize<List<IssProjeDto>>(tableProj.GetRawText(), opts);
                            dto = list?.FirstOrDefault();
                        }
                        else 
                        {
                             // Try direct deserialize if it matches schema
                             try {
                                dto = JsonSerializer.Deserialize<IssProjeDto>(root.GetRawText(), opts);
                             } catch {}
                        }
                    } 
                    else if (root.ValueKind == JsonValueKind.Array)
                    {
                         var list = JsonSerializer.Deserialize<List<IssProjeDto>>(root.GetRawText(), opts);
                         dto = list?.FirstOrDefault();
                    }

                    if (dto != null)
                    {
                        project.Name = dto.ProjeAdi ?? project.Name;
                        project.InstitutionCode = dto.KurumKodu ?? project.InstitutionCode;
                        project.Address = dto.MalzemeTeslimAdresi ?? project.Address;
                        project.LastSyncedAt = DateTime.UtcNow;
                        syncedCount++;
                    }
                    else 
                    {
                        _logger.LogInformation("Project {ProjectCode} DTO IS NULL after parsing.", project.Code);
                        project.LastSyncedAt = DateTime.UtcNow; 
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue
                    _logger.LogError(ex, "SYNC PROJECT ERROR ({ProjectCode})", project.Code);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return syncedCount;
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
