using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.ClothingPicking
{
    public record PickingQueueItemDto(
        int ShipmentId,
        string? ExternalOrderNumber,
        string? TalepNo,
        string ProjectCode,
        string ProjectName,
        int? PickingGroupId,
        int QueueOrder,
        int LineCount,
        int? ReservedForUserId,
        bool ReservedForMe,
        int? AssignedPickerId,
        string? AssignedPickerName,
        string Status,
        int? PickingMode,
        bool Paused);

    public record PickingQueueDto(
        IReadOnlyList<PickingQueueItemDto> Claimable,   // alınabilir (Created, claim'siz) — gruba göre
        IReadOnlyList<PickingQueueItemDto> Mine);        // benim devam eden işlerim (Picking)

    /// <summary>
    /// Toplayıcı kuyruğu. groupId verilirse o grubun alınabilir işleri; null ise
    /// "Gruplandırılmamış" havuz. Rezerve işler yalnızca ilgili kullanıcıya gösterilir.
    /// Ayrıca kullanıcının kendi devam eden (Picking) işleri döner.
    /// </summary>
    public record GetPickingQueueQuery(int? GroupId) : IRequest<PickingQueueDto>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager", "Accounting", "Warehouse" };
    }

    public class GetPickingQueueQueryHandler : IRequestHandler<GetPickingQueueQuery, PickingQueueDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetPickingQueueQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<PickingQueueDto> Handle(GetPickingQueueQuery request, CancellationToken ct)
        {
            var uid = _currentUser.UserId;

            // Alınabilir: kıyafet + Created + claim'siz + (gruba göre) + rezervasyon görünürlüğü
            var claimable = await _context.Shipments
                .Where(s => s.OperationType == OperationType.Clothing
                         && s.Status == ShipmentStatus.Created
                         && s.AssignedPickerId == null
                         && s.PickingGroupId == request.GroupId
                         && (s.ReservedForUserId == null || s.ReservedForUserId == uid))
                .Include(s => s.Project)
                .Include(s => s.IssOrder)
                .OrderBy(s => s.QueueOrder).ThenBy(s => s.DeliveryDate)
                .Select(s => new PickingQueueItemDto(
                    s.Id,
                    s.IssOrder != null ? s.IssOrder.ExternalOrderNumber : null,
                    s.TalepNo,
                    s.Project.Code,
                    s.Project.Name,
                    s.PickingGroupId,
                    s.QueueOrder,
                    s.Lines.Count,
                    s.ReservedForUserId,
                    s.ReservedForUserId != null && s.ReservedForUserId == uid,
                    s.AssignedPickerId,
                    s.AssignedPickerName,
                    s.Status.ToString(),
                    (int?)s.PickingMode,
                    s.PickingPausedAt != null))
                .ToListAsync(ct);

            // Benim devam eden işlerim (tüm gruplar)
            var mine = await _context.Shipments
                .Where(s => s.OperationType == OperationType.Clothing
                         && s.Status == ShipmentStatus.Picking
                         && s.AssignedPickerId == uid)
                .Include(s => s.Project)
                .Include(s => s.IssOrder)
                .OrderBy(s => s.ClaimedAt)
                .Select(s => new PickingQueueItemDto(
                    s.Id,
                    s.IssOrder != null ? s.IssOrder.ExternalOrderNumber : null,
                    s.TalepNo,
                    s.Project.Code,
                    s.Project.Name,
                    s.PickingGroupId,
                    s.QueueOrder,
                    s.Lines.Count,
                    s.ReservedForUserId,
                    s.ReservedForUserId != null && s.ReservedForUserId == uid,
                    s.AssignedPickerId,
                    s.AssignedPickerName,
                    s.Status.ToString(),
                    (int?)s.PickingMode,
                    s.PickingPausedAt != null))
                .ToListAsync(ct);

            return new PickingQueueDto(claimable, mine);
        }
    }
}
