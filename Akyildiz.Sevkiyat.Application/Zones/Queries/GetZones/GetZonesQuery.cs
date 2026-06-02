using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Zones.Queries.GetZones
{
    public record ZoneDto(int Id, string Name, int Order, bool IsOutOfCity, bool IsActive);

    /// <summary>
    /// Varsayılan olarak yalnızca aktif bölgeleri döndürür. Yönetim ekranı için
    /// <paramref name="IncludeInactive"/> = true ile pasifler de dahil edilir.
    /// </summary>
    public record GetZonesQuery(bool IncludeInactive = false) : IRequest<List<ZoneDto>>;

    public class GetZonesQueryHandler : IRequestHandler<GetZonesQuery, List<ZoneDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetZonesQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ZoneDto>> Handle(GetZonesQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Zones.AsQueryable();

            if (!request.IncludeInactive)
                query = query.Where(z => z.IsActive);

            return await query
                .OrderBy(z => z.Name)
                .Select(z => new ZoneDto(z.Id, z.Name, z.Order, z.IsOutOfCity, z.IsActive))
                .ToListAsync(cancellationToken);
        }
    }
}
