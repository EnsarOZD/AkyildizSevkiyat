using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Zones.Queries.GetZones
{
    public record ZoneDto(int Id, string Name, int Order, bool IsOutOfCity);

    public record GetZonesQuery : IRequest<List<ZoneDto>>;

    public class GetZonesQueryHandler : IRequestHandler<GetZonesQuery, List<ZoneDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetZonesQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ZoneDto>> Handle(GetZonesQuery request, CancellationToken cancellationToken)
        {
            return await _context.Zones
                .OrderBy(z => z.Name)
                .Select(z => new ZoneDto(z.Id, z.Name, z.Order, z.IsOutOfCity))
                .ToListAsync(cancellationToken);
        }
    }
}
