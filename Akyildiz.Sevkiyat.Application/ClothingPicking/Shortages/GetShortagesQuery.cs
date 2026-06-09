using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.ClothingPicking.Shortages
{
    public record ShortageDto(
        int Id,
        int ShipmentId,
        string? ExternalOrderNumber,
        int? ShipmentLineId,
        int? StockMasterId,
        string StockCode,
        string StockName,
        int ProjectId,
        string ProjectName,
        decimal Qty,
        int Status,
        string StatusName,
        string? Note,
        DateTime CreatedAt,
        int? FollowupShipmentId);

    /// <summary>Eksik ürün kuyruğu. Varsayılan Pending; proje/durum filtreli.</summary>
    public record GetShortagesQuery(ShortageStatus? Status = ShortageStatus.Pending, int? ProjectId = null)
        : IRequest<List<ShortageDto>>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager", "Accounting", "Warehouse" };
    }

    public class GetShortagesQueryHandler : IRequestHandler<GetShortagesQuery, List<ShortageDto>>
    {
        private readonly IApplicationDbContext _context;
        public GetShortagesQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<List<ShortageDto>> Handle(GetShortagesQuery request, CancellationToken ct)
        {
            var q = _context.ShortageRecords.AsQueryable();
            if (request.Status.HasValue) q = q.Where(r => r.Status == request.Status.Value);
            if (request.ProjectId.HasValue) q = q.Where(r => r.ProjectId == request.ProjectId.Value);

            return await q
                .Include(r => r.Shipment).ThenInclude(s => s.IssOrder)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ShortageDto(
                    r.Id,
                    r.ShipmentId,
                    r.Shipment.IssOrder != null ? r.Shipment.IssOrder.ExternalOrderNumber : null,
                    r.ShipmentLineId,
                    r.StockMasterId,
                    r.StockCode,
                    r.StockName,
                    r.ProjectId,
                    r.ProjectName,
                    r.Qty,
                    (int)r.Status,
                    r.Status.ToString(),
                    r.Note,
                    r.CreatedAt,
                    r.FollowupShipmentId))
                .ToListAsync(ct);
        }
    }
}
