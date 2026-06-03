using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.FreightDeliveries.Queries.GetActiveFreightDeliveries
{
    /// <summary>
    /// Nakliyeci teslim linklerini listeler. Varsayılan olarak yalnızca aktif
    /// (tamamlanmamış ve süresi dolmamış) linkleri döndürür; tekrar göndermek için kullanılır.
    /// </summary>
    public record GetActiveFreightDeliveriesQuery(bool IncludeInactive = false)
        : IRequest<List<FreightDeliveryListItemDto>>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting", "Warehouse" };
    }

    public record FreightDeliveryListItemDto(
        string Token,
        int ProjectId,
        string ProjectName,
        string CarrierName,
        string? CarrierPhone,
        int ShipmentCount,
        DateTime CreatedAt,
        DateTime ExpiresAt,
        bool IsCompleted,
        bool IsExpired,
        DateTime? CompletedAt,
        string? RecipientName
    );

    public class GetActiveFreightDeliveriesQueryHandler
        : IRequestHandler<GetActiveFreightDeliveriesQuery, List<FreightDeliveryListItemDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetActiveFreightDeliveriesQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<FreightDeliveryListItemDto>> Handle(
            GetActiveFreightDeliveriesQuery request, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;

            var query = _context.FreightDeliveries
                .Include(d => d.Project)
                .Include(d => d.Shipments)
                .AsNoTracking()
                .AsQueryable();

            if (!request.IncludeInactive)
                query = query.Where(d => d.CompletedAt == null && d.ExpiresAt > now);

            var list = await query
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new FreightDeliveryListItemDto(
                    d.Token,
                    d.ProjectId,
                    d.Project.Name,
                    d.CarrierName,
                    d.CarrierPhone,
                    d.Shipments.Count,
                    d.CreatedAt,
                    d.ExpiresAt,
                    d.CompletedAt != null,
                    d.ExpiresAt <= now,
                    d.CompletedAt,
                    d.RecipientName))
                .ToListAsync(cancellationToken);

            return list;
        }
    }
}
