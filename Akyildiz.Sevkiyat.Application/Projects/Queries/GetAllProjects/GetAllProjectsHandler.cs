using Akyildiz.Sevkiyat.Application.Common.Dtos;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Projects.Queries
{
    public class GetAllProjectsQueryHandler
        : IRequestHandler<GetAllProjectsQuery, List<ProjectDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetAllProjectsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ProjectDto>> Handle(GetAllProjectsQuery request, CancellationToken cancellationToken)
        {
            return await _context.Projects
                .Include(p => p.Zone)
                .Select(p => new ProjectDto(
                    p.Id,
                    p.Code,
                    p.Name,
                    p.Region,
                    p.IsActive,
                    p.ZoneId,
                    p.Zone != null ? p.Zone.Name : null,
                    p.NetsisCariKodu,
                    p.DeliveryOrder,
                    p.Latitude,
                    p.Longitude
                )).ToListAsync(cancellationToken);
        }
    }
}
