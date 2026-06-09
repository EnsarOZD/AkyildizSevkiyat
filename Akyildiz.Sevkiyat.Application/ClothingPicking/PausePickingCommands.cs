using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.ClothingPicking
{
    // Duraklat — ContainerAssignment'lar AÇIK kalır; kısmi toplama korunur.
    public record PausePickingCommand(int ShipmentId) : IRequest<Unit>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager", "Accounting", "Warehouse" };
    }

    public class PausePickingCommandHandler : IRequestHandler<PausePickingCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        public PausePickingCommandHandler(IApplicationDbContext c, ICurrentUserService u) { _context = c; _currentUser = u; }

        public async Task<Unit> Handle(PausePickingCommand request, CancellationToken ct)
        {
            var s = await _context.Shipments.FirstOrDefaultAsync(x => x.Id == request.ShipmentId, ct)
                ?? throw new NotFoundException("Shipment", request.ShipmentId);
            ClothingPickingGuards.EnsureOwnerOrManager(s, _currentUser);
            if (s.Status != ShipmentStatus.Picking)
                throw new DomainException("Yalnızca 'Hazırlanıyor' durumundaki iş duraklatılabilir.");

            s.PickingPausedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return Unit.Value;
        }
    }

    public record ResumePickingCommand(int ShipmentId) : IRequest<Unit>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager", "Accounting", "Warehouse" };
    }

    public class ResumePickingCommandHandler : IRequestHandler<ResumePickingCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        public ResumePickingCommandHandler(IApplicationDbContext c, ICurrentUserService u) { _context = c; _currentUser = u; }

        public async Task<Unit> Handle(ResumePickingCommand request, CancellationToken ct)
        {
            var s = await _context.Shipments.FirstOrDefaultAsync(x => x.Id == request.ShipmentId, ct)
                ?? throw new NotFoundException("Shipment", request.ShipmentId);
            ClothingPickingGuards.EnsureOwnerOrManager(s, _currentUser);

            s.PickingPausedAt = null;
            await _context.SaveChangesAsync(ct);
            return Unit.Value;
        }
    }
}
