using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.InstitutionCariMappings.Commands.ApplyMappingsToProjects
{
    /// <summary>
    /// Tanımlı InstitutionCariMapping kayıtlarını mevcut ISS projelerine uygular.
    /// <see cref="DryRun"/> = true ise sadece etkilenecek projelerin listesini döner,
    /// veritabanı değişikliği yapmaz. false ise aynı listeyi döner ve değişiklikleri kaydeder.
    ///
    /// Sadece Source = Iss projeler kapsama girer. Manuel müşterilerin NetsisCariKodu'su
    /// kullanıcı tarafından elle yönetildiği için bu komut onlara dokunmaz.
    /// </summary>
    public record ApplyMappingsToProjectsCommand(bool DryRun)
        : IRequest<ApplyMappingsToProjectsResult>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager", "Accounting" };
    }

    public record ApplyMappingsToProjectsResult(
        int TotalProjectsScanned,
        int AffectedCount,
        int UnchangedCount,
        int NoMappingCount,
        IReadOnlyList<ProjectChangePreview> Changes,
        IReadOnlyList<ProjectChangePreview> WithoutMapping,
        bool DryRun
    );

    public record ProjectChangePreview(
        int ProjectId,
        string ProjectCode,
        string ProjectName,
        string? InstitutionCode,
        string? CurrentNetsisCariKodu,
        string? NewNetsisCariKodu,
        string? MappingDescription
    );

    public class ApplyMappingsToProjectsCommandHandler
        : IRequestHandler<ApplyMappingsToProjectsCommand, ApplyMappingsToProjectsResult>
    {
        private readonly IApplicationDbContext _context;

        public ApplyMappingsToProjectsCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ApplyMappingsToProjectsResult> Handle(
            ApplyMappingsToProjectsCommand request, CancellationToken cancellationToken)
        {
            var activeMappings = await _context.InstitutionCariMappings
                .Where(m => m.IsActive)
                .ToDictionaryAsync(m => m.InstitutionCode, cancellationToken);

            var projects = await _context.Projects
                .Where(p => p.Source == ProjectSource.Iss)
                .ToListAsync(cancellationToken);

            var changes = new List<ProjectChangePreview>();
            var withoutMapping = new List<ProjectChangePreview>();
            int unchanged = 0;

            foreach (var p in projects)
            {
                if (string.IsNullOrWhiteSpace(p.InstitutionCode))
                {
                    // KurumKodu yoksa eşleşme yapamayız
                    if (string.IsNullOrWhiteSpace(p.NetsisCariKodu))
                        withoutMapping.Add(new ProjectChangePreview(
                            p.Id, p.Code, p.Name, p.InstitutionCode,
                            p.NetsisCariKodu, null, null));
                    else
                        unchanged++;
                    continue;
                }

                if (!activeMappings.TryGetValue(p.InstitutionCode, out var mapping))
                {
                    withoutMapping.Add(new ProjectChangePreview(
                        p.Id, p.Code, p.Name, p.InstitutionCode,
                        p.NetsisCariKodu, null, null));
                    continue;
                }

                if (string.Equals(p.NetsisCariKodu, mapping.NetsisCariKodu, StringComparison.Ordinal))
                {
                    unchanged++;
                    continue;
                }

                changes.Add(new ProjectChangePreview(
                    p.Id, p.Code, p.Name, p.InstitutionCode,
                    p.NetsisCariKodu, mapping.NetsisCariKodu, mapping.Description));

                if (!request.DryRun)
                    p.NetsisCariKodu = mapping.NetsisCariKodu;
            }

            if (!request.DryRun && changes.Count > 0)
                await _context.SaveChangesAsync(cancellationToken);

            return new ApplyMappingsToProjectsResult(
                TotalProjectsScanned: projects.Count,
                AffectedCount: changes.Count,
                UnchangedCount: unchanged,
                NoMappingCount: withoutMapping.Count,
                Changes: changes,
                WithoutMapping: withoutMapping,
                DryRun: request.DryRun
            );
        }
    }
}
