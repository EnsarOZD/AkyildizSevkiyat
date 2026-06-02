using Akyildiz.Sevkiyat.Application.Common.Dtos;
using Akyildiz.Sevkiyat.Application.Common.Models;
using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Projects.Queries
{
    public class GetAllProjectsQueryHandler
        : IRequestHandler<GetAllProjectsQuery, PaginatedList<ProjectDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetAllProjectsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedList<ProjectDto>> Handle(GetAllProjectsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Projects
                .Where(p => p.Source == Domain.Enums.ProjectSource.Iss)
                .Include(p => p.Zone)
                .AsQueryable();

            if (!request.ShowInactive)
                query = query.Where(p => p.IsActive);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var s = request.Search.ToLower();
                query = query.Where(p =>
                    p.Code.ToLower().Contains(s) ||
                    p.Name.ToLower().Contains(s));
            }

            var projected = query.OrderBy(p => p.Name).Select(p => new ProjectDto(
                p.Id, p.Code, p.Name, p.Region, p.IsActive,
                p.ZoneId, p.Zone != null ? p.Zone.Name : null,
                p.NetsisCariKodu, p.DeliveryOrder,
                p.Latitude, p.Longitude, p.Address,
                p.DeliveryWindowStart, p.DeliveryWindowEnd,
                p.NetsisTeslimCariKodu,
                p.Source
            ));

            return await PaginatedList<ProjectDto>.CreateAsync(projected, request.PageNumber, request.PageSize);
        }
    }
}
