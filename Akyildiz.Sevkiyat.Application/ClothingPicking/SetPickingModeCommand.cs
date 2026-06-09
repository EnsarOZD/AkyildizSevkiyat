using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.ClothingPicking
{
    /// <summary>Claim sonrası toplama modunu seçer (Cart | Pallet | Handheld).</summary>
    public record SetPickingModeCommand(int ShipmentId, PickingMode Mode) : IRequest<Unit>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager", "Accounting", "Warehouse" };
    }

    public class SetPickingModeCommandHandler : IRequestHandler<SetPickingModeCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public SetPickingModeCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Unit> Handle(SetPickingModeCommand request, CancellationToken ct)
        {
            var s = await _context.Shipments
                .FirstOrDefaultAsync(x => x.Id == request.ShipmentId, ct)
                ?? throw new NotFoundException("Shipment", request.ShipmentId);

            ClothingPickingGuards.EnsureOwnerOrManager(s, _currentUser);

            if (s.Status != ShipmentStatus.Picking)
                throw new DomainException("Toplama modu yalnızca 'Hazırlanıyor' durumunda seçilebilir.");

            s.PickingMode = request.Mode;
            await _context.SaveChangesAsync(ct);
            return Unit.Value;
        }
    }
}
