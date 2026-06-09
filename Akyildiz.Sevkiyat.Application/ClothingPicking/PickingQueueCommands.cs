using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.ClothingPicking
{
    // ── Gruba atama (null = "Gruplandırılmamış" havuzu) ─────────────────────
    public record AssignShipmentsToGroupCommand(List<int> ShipmentIds, int? PickingGroupId)
        : IRequest<int>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager" };
    }

    public class AssignShipmentsToGroupCommandHandler : IRequestHandler<AssignShipmentsToGroupCommand, int>
    {
        private readonly IApplicationDbContext _context;
        public AssignShipmentsToGroupCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<int> Handle(AssignShipmentsToGroupCommand request, CancellationToken ct)
        {
            if (request.PickingGroupId.HasValue)
            {
                var exists = await _context.PickingGroups.AnyAsync(g => g.Id == request.PickingGroupId && g.IsActive, ct);
                if (!exists) throw new NotFoundException("PickingGroup", request.PickingGroupId.Value);
            }

            // Yalnızca kıyafet + henüz claim edilmemiş (Created) sevkiyatlar gruplanır.
            var shipments = await _context.Shipments
                .Where(s => request.ShipmentIds.Contains(s.Id)
                         && s.OperationType == OperationType.Clothing
                         && s.Status == ShipmentStatus.Created)
                .ToListAsync(ct);

            foreach (var s in shipments)
                s.PickingGroupId = request.PickingGroupId;

            await _context.SaveChangesAsync(ct);
            return shipments.Count;
        }
    }

    // ── Grup içi sıralama (araya sokma dahil) ───────────────────────────────
    public record ReorderPickingQueueCommand(List<int> OrderedShipmentIds) : IRequest<Unit>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager" };
    }

    public class ReorderPickingQueueCommandHandler : IRequestHandler<ReorderPickingQueueCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        public ReorderPickingQueueCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Unit> Handle(ReorderPickingQueueCommand request, CancellationToken ct)
        {
            var ids = request.OrderedShipmentIds;
            var shipments = await _context.Shipments
                .Where(s => ids.Contains(s.Id) && s.OperationType == OperationType.Clothing)
                .ToListAsync(ct);

            // 10'ar artışla sıra ver (araya elle sokma/yeniden sıralama için boşluk bırak).
            for (int i = 0; i < ids.Count; i++)
            {
                var s = shipments.FirstOrDefault(x => x.Id == ids[i]);
                if (s != null) s.QueueOrder = (i + 1) * 10;
            }

            await _context.SaveChangesAsync(ct);
            return Unit.Value;
        }
    }

    // ── Belirli toplayıcıya rezerve etme (null = rezervasyonu kaldır) ────────
    public record ReserveShipmentForPickerCommand(int ShipmentId, int? ReservedForUserId)
        : IRequest<Unit>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager" };
    }

    public class ReserveShipmentForPickerCommandHandler : IRequestHandler<ReserveShipmentForPickerCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        public ReserveShipmentForPickerCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Unit> Handle(ReserveShipmentForPickerCommand request, CancellationToken ct)
        {
            var s = await _context.Shipments
                .FirstOrDefaultAsync(x => x.Id == request.ShipmentId && x.OperationType == OperationType.Clothing, ct)
                ?? throw new NotFoundException("Shipment", request.ShipmentId);

            if (s.Status != ShipmentStatus.Created)
                throw new DomainException("Yalnızca henüz alınmamış (Created) sevkiyat rezerve edilebilir.");

            s.ReservedForUserId = request.ReservedForUserId;
            await _context.SaveChangesAsync(ct);
            return Unit.Value;
        }
    }
}
