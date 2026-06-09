using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.ClothingPicking
{
    /// <summary>
    /// Toplayıcının grup içinden bir işi ÜSTLENMESİ (pull/claim). Atomik — Shipment.RowVersion
    /// ile çifte claim engellenir (driver panel concurrency emsali). Sıra atlanırsa loglanır.
    /// </summary>
    public record ClaimShipmentCommand(int ShipmentId) : IRequest<Unit>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager", "Accounting", "Warehouse" };
    }

    public class ClaimShipmentCommandHandler : IRequestHandler<ClaimShipmentCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public ClaimShipmentCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Unit> Handle(ClaimShipmentCommand request, CancellationToken ct)
        {
            var s = await _context.Shipments
                .FirstOrDefaultAsync(x => x.Id == request.ShipmentId, ct)
                ?? throw new NotFoundException("Shipment", request.ShipmentId);

            if (s.OperationType != OperationType.Clothing)
                throw new DomainException("Bu işlem yalnızca kıyafet sevkiyatları içindir.");

            if (s.AssignedPickerId != null || s.Status != ShipmentStatus.Created)
                throw new ConflictException("Bu iş zaten alınmış veya hazırlığa başlanmış.");

            // Rezervasyon: doluysa yalnızca o kullanıcı claim edebilir.
            if (s.ReservedForUserId.HasValue && s.ReservedForUserId != _currentUser.UserId)
                throw new ForbiddenException("Bu iş başka bir toplayıcı için ayrılmış.");

            // Sıra atlama tespiti: aynı grupta, claim edilebilir, daha üst sırada (küçük QueueOrder)
            // ve bu kullanıcıya kapalı olmayan bir iş varsa → sıra atlanmış sayılır.
            bool hasHigherPriority = await _context.Shipments.AnyAsync(x =>
                x.OperationType == OperationType.Clothing &&
                x.Status == ShipmentStatus.Created &&
                x.AssignedPickerId == null &&
                x.Id != s.Id &&
                x.PickingGroupId == s.PickingGroupId &&
                x.QueueOrder < s.QueueOrder &&
                (x.ReservedForUserId == null || x.ReservedForUserId == _currentUser.UserId), ct);

            s.AssignedPickerId = _currentUser.UserId;
            s.AssignedPickerName = _currentUser.FullName ?? _currentUser.Email;
            s.ClaimedAt = DateTime.UtcNow;
            s.ClaimedOutOfOrder = hasHigherPriority;

            // Created → Picking (skip geçişi; sebep zorunlu)
            s.ChangeStatus(ShipmentStatus.Picking, _currentUser.UserId,
                hasHigherPriority ? "Toplayıcı işi aldı (sıra atlandı)" : "Toplayıcı işi aldı");

            try
            {
                await _context.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Aynı anda başka toplayıcı aldı → kaybeden tarafa anlamlı hata
                throw new ConflictException("Bu iş az önce başka bir toplayıcı tarafından alındı.");
            }

            return Unit.Value;
        }
    }
}
