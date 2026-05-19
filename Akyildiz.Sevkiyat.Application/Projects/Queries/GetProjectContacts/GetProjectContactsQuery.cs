using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Projects.Queries.GetProjectContacts
{
    public record GetProjectContactsQuery : IRequest<List<ProjectContactDto>>;

    public record ProjectContactDto(
        int Id,
        string Code,
        string Name,
        string? Zone,
        string? DefaultContactName,
        string? DefaultContactPhone
    );

    public class GetProjectContactsQueryHandler : IRequestHandler<GetProjectContactsQuery, List<ProjectContactDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetProjectContactsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ProjectContactDto>> Handle(GetProjectContactsQuery request, CancellationToken cancellationToken)
        {
            return await _context.Projects
                .Include(p => p.Zone)
                .Where(p => p.IsActive)
                .OrderBy(p => p.Zone != null ? p.Zone.Name : "zzz")
                .ThenBy(p => p.Name)
                .Select(p => new ProjectContactDto(
                    p.Id,
                    p.Code,
                    p.Name,
                    p.Zone != null ? p.Zone.Name : null,
                    p.DefaultContactName,
                    p.DefaultContactPhone
                ))
                .ToListAsync(cancellationToken);
        }
    }
}
