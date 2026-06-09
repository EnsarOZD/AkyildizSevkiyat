using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.ClothingPicking
{
    public record ClosingQueueItemDto(
        int ShipmentId,
        string? ExternalOrderNumber,
        string? TalepNo,
        string ProjectName,
        int? PickingMode,
        int LineCount);

    /// <summary>Kapamaya hazır kuyruk: toplama bitmiş (PickingCompletedAt) ama henüz kapanmamış (ClosedAt boş).</summary>
    public record GetClosingQueueQuery() : IRequest<List<ClosingQueueItemDto>>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager", "Accounting", "Warehouse" };
    }

    public class GetClosingQueueQueryHandler : IRequestHandler<GetClosingQueueQuery, List<ClosingQueueItemDto>>
    {
        private readonly IApplicationDbContext _context;
        public GetClosingQueueQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<List<ClosingQueueItemDto>> Handle(GetClosingQueueQuery request, CancellationToken ct)
            => await _context.Shipments
                .Where(s => s.OperationType == OperationType.Clothing
                         && s.Status == ShipmentStatus.Picking
                         && s.PickingCompletedAt != null
                         && s.ClosedAt == null)
                .Include(s => s.Project)
                .Include(s => s.IssOrder)
                .OrderBy(s => s.PickingCompletedAt)
                .Select(s => new ClosingQueueItemDto(
                    s.Id,
                    s.IssOrder != null ? s.IssOrder.ExternalOrderNumber : null,
                    s.TalepNo,
                    s.Project.Name,
                    (int?)s.PickingMode,
                    s.Lines.Count))
                .ToListAsync(ct);
    }
}
