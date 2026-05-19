using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Reports.Queries.GetWarehouseStockDistribution
{
    public record GetWarehouseStockDistributionQuery() : IRequest<WarehouseStockDistributionDto>;

    public class WarehouseStockDistributionDto
    {
        public List<LocationTypeSummary> Summary { get; set; } = [];
        public List<LocationStockRow>   Details  { get; set; } = [];
    }

    public record LocationTypeSummary(
        int     LocationTypeId,
        string  LocationTypeLabel,
        decimal TotalOnHand,
        decimal TotalReserved,
        decimal TotalAvailable,
        int     LocationCount,
        int     StockCount
    );

    public record LocationStockRow(
        int     WarehouseLocationId,
        string  LocationCode,
        int     LocationTypeId,
        string  LocationTypeLabel,
        string? Alan,
        int     StockMasterId,
        string  StockCode,
        string  StockName,
        string  Unit,
        decimal OnHandQty,
        decimal ReservedQty,
        decimal AvailableQty
    );

    public class GetWarehouseStockDistributionQueryHandler
        : IRequestHandler<GetWarehouseStockDistributionQuery, WarehouseStockDistributionDto>
    {
        private static readonly Dictionary<LocationType, string> TypeLabels = new()
        {
            { LocationType.Rack,        "Raf"           },
            { LocationType.FloorStack,  "Zemin İstif"  },
            { LocationType.Receiving,   "Mal Kabul"    },
            { LocationType.Shipping,    "Sevkiyat"     },
            { LocationType.Quarantine,  "Karantina"    },
            { LocationType.Staging,     "Hazırlama"    },
            { LocationType.PickingFace, "Toplama Gözü" },
            { LocationType.Returns,     "İade"         },
        };

        private readonly IApplicationDbContext _context;

        public GetWarehouseStockDistributionQueryHandler(IApplicationDbContext context)
            => _context = context;

        public async Task<WarehouseStockDistributionDto> Handle(
            GetWarehouseStockDistributionQuery request, CancellationToken cancellationToken)
        {
            var rows = await _context.StockLocations
                .Where(sl => sl.OnHandQty > 0 || sl.ReservedQty > 0)
                .Select(sl => new
                {
                    sl.WarehouseLocationId,
                    LocationCode     = sl.WarehouseLocation.Code,
                    LocationType     = sl.WarehouseLocation.LocationType,
                    Alan             = sl.WarehouseLocation.Alan,
                    sl.StockMasterId,
                    StockCode        = sl.StockMaster.StockCode,
                    StockName        = sl.StockMaster.StockName,
                    Unit             = sl.StockMaster.Unit.ToString(),
                    sl.OnHandQty,
                    sl.ReservedQty,
                })
                .ToListAsync(cancellationToken);

            var details = rows.Select(r => new LocationStockRow(
                r.WarehouseLocationId,
                r.LocationCode,
                (int)r.LocationType,
                TypeLabels.TryGetValue(r.LocationType, out var lbl) ? lbl : r.LocationType.ToString(),
                r.Alan,
                r.StockMasterId,
                r.StockCode,
                r.StockName,
                r.Unit,
                r.OnHandQty,
                r.ReservedQty,
                r.OnHandQty - r.ReservedQty
            ))
            .OrderBy(r => r.LocationTypeId)
            .ThenBy(r => r.LocationCode)
            .ThenBy(r => r.StockCode)
            .ToList();

            var summary = details
                .GroupBy(r => r.LocationTypeId)
                .Select(g => new LocationTypeSummary(
                    g.Key,
                    g.First().LocationTypeLabel,
                    g.Sum(r => r.OnHandQty),
                    g.Sum(r => r.ReservedQty),
                    g.Sum(r => r.AvailableQty),
                    g.Select(r => r.WarehouseLocationId).Distinct().Count(),
                    g.Select(r => r.StockMasterId).Distinct().Count()
                ))
                .OrderBy(s => s.LocationTypeId)
                .ToList();

            return new WarehouseStockDistributionDto { Summary = summary, Details = details };
        }
    }
}
