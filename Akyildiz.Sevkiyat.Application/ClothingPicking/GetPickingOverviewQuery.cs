using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.ClothingPicking
{
    public record PickingOverviewItemDto(
        int ShipmentId,
        string? ExternalOrderNumber,
        string? TalepNo,
        string ProjectCode,
        string ProjectName,
        int? PickingGroupId,
        int QueueOrder,
        int? ReservedForUserId,
        int? AssignedPickerId,
        string? AssignedPickerName,
        string Status,
        int? PickingMode,
        bool ClaimedOutOfOrder,
        bool Paused,
        bool PickingCompleted,
        int OpenContainerCount,
        int LineCount);

    public record PickingGroupOverviewDto(int Id, string Name, int SortOrder, bool IsActive);

    public record PickingOverviewDto(
        IReadOnlyList<PickingGroupOverviewDto> Groups,
        IReadOnlyList<PickingOverviewItemDto> Items);

    /// <summary>
    /// Yönetici panosu: tüm kıyafet sevkiyatları (Created/Picking) — grup, sıra, claim,
    /// mod, bayraklar ve açık araba sayısıyla. Tek sorguda board verisi.
    /// </summary>
    public record GetPickingOverviewQuery() : IRequest<PickingOverviewDto>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager" };
    }

    public class GetPickingOverviewQueryHandler : IRequestHandler<GetPickingOverviewQuery, PickingOverviewDto>
    {
        private readonly IApplicationDbContext _context;
        public GetPickingOverviewQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<PickingOverviewDto> Handle(GetPickingOverviewQuery request, CancellationToken ct)
        {
            var groups = await _context.PickingGroups
                .OrderBy(g => g.SortOrder).ThenBy(g => g.Name)
                .Select(g => new PickingGroupOverviewDto(g.Id, g.Name, g.SortOrder, g.IsActive))
                .ToListAsync(ct);

            var statuses = new[] { ShipmentStatus.Created, ShipmentStatus.Picking };

            var items = await _context.Shipments
                .Where(s => s.OperationType == OperationType.Clothing && statuses.Contains(s.Status))
                .Include(s => s.Project)
                .Include(s => s.IssOrder)
                .OrderBy(s => s.PickingGroupId).ThenBy(s => s.QueueOrder)
                .Select(s => new PickingOverviewItemDto(
                    s.Id,
                    s.IssOrder != null ? s.IssOrder.ExternalOrderNumber : null,
                    s.TalepNo,
                    s.Project.Code,
                    s.Project.Name,
                    s.PickingGroupId,
                    s.QueueOrder,
                    s.ReservedForUserId,
                    s.AssignedPickerId,
                    s.AssignedPickerName,
                    s.Status.ToString(),
                    (int?)s.PickingMode,
                    s.ClaimedOutOfOrder,
                    s.PickingPausedAt != null,
                    s.PickingCompletedAt != null,
                    _context.ContainerAssignments.Count(a => a.ShipmentId == s.Id && a.ReleasedAt == null),
                    s.Lines.Count))
                .ToListAsync(ct);

            return new PickingOverviewDto(groups, items);
        }
    }
}
