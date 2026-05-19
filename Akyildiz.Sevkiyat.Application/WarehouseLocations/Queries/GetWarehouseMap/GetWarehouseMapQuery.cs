using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.WarehouseLocations.Queries.GetWarehouseMap
{
    /// <summary>
    /// 2D depo haritası için modül bazında özet veri döner.
    /// Her (KoridorNo, Taraf, ModulNo) kombinasyonu için tek bir hücre kaydı üretilir.
    /// </summary>
    public record GetWarehouseMapQuery : IRequest<List<MapModuleDto>>;

    public record MapModuleDto(
        int    KoridorNo,
        string Taraf,
        int    ModulNo,
        int    DominantTypeId,  // en çok tekrar eden LocationType
        bool   HasActive,
        bool   AllActive,
        int    TotalLocations
    );

    public class GetWarehouseMapQueryHandler : IRequestHandler<GetWarehouseMapQuery, List<MapModuleDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetWarehouseMapQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<MapModuleDto>> Handle(GetWarehouseMapQuery request, CancellationToken cancellationToken)
        {
            var raw = await _context.WarehouseLocations
                .AsNoTracking()
                .Select(l => new
                {
                    l.KoridorNo,
                    l.Taraf,
                    l.ModulNo,
                    l.LocationType,
                    l.IsActive,
                })
                .ToListAsync(cancellationToken);

            var grouped = raw
                .GroupBy(l => (l.KoridorNo, l.Taraf, l.ModulNo))
                .Select(g =>
                {
                    var dominantType = g
                        .GroupBy(x => x.LocationType)
                        .OrderByDescending(x => x.Count())
                        .First().Key;

                    return new MapModuleDto(
                        g.Key.KoridorNo,
                        g.Key.Taraf,
                        g.Key.ModulNo,
                        (int)dominantType,
                        g.Any(x => x.IsActive),
                        g.All(x => x.IsActive),
                        g.Count()
                    );
                })
                .OrderBy(m => m.KoridorNo)
                .ThenBy(m => m.Taraf)
                .ThenBy(m => m.ModulNo)
                .ToList();

            return grouped;
        }
    }
}
