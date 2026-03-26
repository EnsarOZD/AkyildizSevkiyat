using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.StockLocations.Queries.GetStockLocations
{
    /// <summary>
    /// Bir stok için tüm lokasyonları döner.
    /// stockMasterId = null ise tüm stok-lokasyon satırlarını döner.
    /// </summary>
    public record GetStockLocationsQuery(
        int? StockMasterId = null,
        int? WarehouseLocationId = null
    ) : IRequest<IReadOnlyList<StockLocationDto>>;

    public record StockLocationDto(
        int    Id,
        int    StockMasterId,
        string StockCode,
        string StockName,
        string Unit,
        int    WarehouseLocationId,
        string LocationCode,
        string LocationPrefix,   // Örn: "1K", "2G"
        string LocationType,
        decimal OnHandQty,
        decimal ReservedQty,
        decimal AvailableQty,
        DateTime? LastMovedAt
    );

    public class GetStockLocationsQueryHandler
        : IRequestHandler<GetStockLocationsQuery, IReadOnlyList<StockLocationDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetStockLocationsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<StockLocationDto>> Handle(
            GetStockLocationsQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.StockLocations
                .Include(sl => sl.StockMaster)
                .Include(sl => sl.WarehouseLocation)
                .AsQueryable();

            if (request.StockMasterId.HasValue)
                query = query.Where(sl => sl.StockMasterId == request.StockMasterId.Value);

            if (request.WarehouseLocationId.HasValue)
                query = query.Where(sl => sl.WarehouseLocationId == request.WarehouseLocationId.Value);

            return await query
                .OrderBy(sl => sl.WarehouseLocation.Code)
                .Select(sl => new StockLocationDto(
                    sl.Id,
                    sl.StockMasterId,
                    sl.StockMaster.StockCode,
                    sl.StockMaster.StockName,
                    sl.StockMaster.Unit.ToString(),
                    sl.WarehouseLocationId,
                    sl.WarehouseLocation.Code,
                    sl.WarehouseLocation.KoridorNo.ToString() + sl.WarehouseLocation.Taraf,
                    sl.WarehouseLocation.LocationType.ToString(),
                    sl.OnHandQty,
                    sl.ReservedQty,
                    sl.OnHandQty - sl.ReservedQty,
                    sl.LastMovedAt
                ))
                .ToListAsync(cancellationToken);
        }
    }
}
