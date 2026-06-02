using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.WarehouseLocations.Queries.GetWarehouseLocations
{
    public record GetWarehouseLocationsQuery(
        int?         KoridorNo       = null,
        string?      Taraf           = null,   // "K" veya "G"
        LocationType? Type           = null,
        bool         IncludeInactive = false,
        int          Page            = 1,
        int          PageSize        = 50,
        LocationType? ExcludeType    = null     // Belirli bir tipi hariç tut (örn. Raflar listesinde PickingFace)
    ) : IRequest<GetWarehouseLocationsResult>;

    public record WarehouseLocationDto(
        int      Id,
        string   Code,
        int      KoridorNo,
        string   Taraf,
        int      ModulNo,
        int      Kat,
        string   LocationType,
        int      LocationTypeId,
        string?  Description,
        decimal? MaxWeightKg,
        int?     MaxPallets,
        bool     IsActive,
        string?  Alan,
        string?  QrCode,
        int?     TotalFloors,
        string?  InnerLevel,
        int?     InnerPosition,
        int      ContainerTypeId
    );

    public record GetWarehouseLocationsResult(
        IReadOnlyList<WarehouseLocationDto> Items,
        int TotalCount,
        int Page,
        int PageSize
    );

    public class GetWarehouseLocationsQueryHandler
        : IRequestHandler<GetWarehouseLocationsQuery, GetWarehouseLocationsResult>
    {
        private readonly IApplicationDbContext _context;

        public GetWarehouseLocationsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GetWarehouseLocationsResult> Handle(
            GetWarehouseLocationsQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.WarehouseLocations.AsQueryable();

            if (!request.IncludeInactive)
                query = query.Where(l => l.IsActive);

            if (request.KoridorNo.HasValue)
                query = query.Where(l => l.KoridorNo == request.KoridorNo.Value);

            if (!string.IsNullOrWhiteSpace(request.Taraf))
                query = query.Where(l => l.Taraf == request.Taraf.Trim().ToUpperInvariant());

            if (request.Type.HasValue)
                query = query.Where(l => l.LocationType == request.Type.Value);

            if (request.ExcludeType.HasValue)
                query = query.Where(l => l.LocationType != request.ExcludeType.Value);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(l => l.KoridorNo)
                .ThenBy(l => l.Taraf)
                .ThenBy(l => l.ModulNo)
                .ThenBy(l => l.Kat)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(l => new WarehouseLocationDto(
                    l.Id,
                    l.Code,
                    l.KoridorNo,
                    l.Taraf,
                    l.ModulNo,
                    l.Kat,
                    l.LocationType.ToString(),
                    (int)l.LocationType,
                    l.Description,
                    l.MaxWeightKg,
                    l.MaxPallets,
                    l.IsActive,
                    l.Alan,
                    l.QrCode,
                    l.TotalFloors,
                    l.InnerLevel,
                    l.InnerPosition,
                    (int)l.ContainerType
                ))
                .ToListAsync(cancellationToken);

            return new GetWarehouseLocationsResult(items, totalCount, request.Page, request.PageSize);
        }
    }
}
