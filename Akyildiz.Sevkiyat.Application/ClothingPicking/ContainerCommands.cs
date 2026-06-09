using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.ClothingPicking
{
    // ── Container bağlama (QR/barkod okutma) ────────────────────────────────
    public record ScanContainerResult(int ContainerAssignmentId, int ContainerId, string Code, int ContainerType);

    public record ScanContainerCommand(int ShipmentId, string Code) : IRequest<ScanContainerResult>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager", "Accounting", "Warehouse" };
    }

    public class ScanContainerCommandHandler : IRequestHandler<ScanContainerCommand, ScanContainerResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public ScanContainerCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<ScanContainerResult> Handle(ScanContainerCommand request, CancellationToken ct)
        {
            var s = await _context.Shipments.FirstOrDefaultAsync(x => x.Id == request.ShipmentId, ct)
                ?? throw new NotFoundException("Shipment", request.ShipmentId);

            ClothingPickingGuards.EnsureOwnerOrManager(s, _currentUser);

            if (s.Status != ShipmentStatus.Picking)
                throw new DomainException("Araba yalnızca 'Hazırlanıyor' durumunda bağlanabilir.");
            if (s.PickingMode != PickingMode.Cart)
                throw new DomainException("Araba bağlama yalnızca Araba (Cart) modunda yapılır.");

            var code = (request.Code ?? string.Empty).Trim();
            var container = await _context.Containers.FirstOrDefaultAsync(c => c.Code == code, ct);
            if (container == null)
                throw new NotFoundException($"'{code}' kodlu araba bulunamadı.");
            if (!container.IsActive)
                throw new DomainException($"'{code}' arabası pasif.");

            // Bu sevkiyata zaten açık bağlıysa idempotent dön
            var existingHere = await _context.ContainerAssignments
                .FirstOrDefaultAsync(a => a.ContainerId == container.Id && a.ShipmentId == s.Id && a.ReleasedAt == null, ct);
            if (existingHere != null)
                return new ScanContainerResult(existingHere.Id, container.Id, container.Code, (int)container.Type);

            // Başka bir sevkiyatta AÇIK assignment varsa reddet
            var openElsewhere = await _context.ContainerAssignments
                .Where(a => a.ContainerId == container.Id && a.ReleasedAt == null)
                .Select(a => (int?)a.ShipmentId)
                .FirstOrDefaultAsync(ct);
            if (openElsewhere.HasValue)
                throw new ConflictException($"'{code}' şu an #{openElsewhere.Value} siparişinde kullanımda.");

            var assignment = new ContainerAssignment
            {
                ContainerId = container.Id,
                ShipmentId = s.Id,
                AssignedAt = DateTime.UtcNow,
                AssignedByUserId = _currentUser.UserId,
            };
            _context.ContainerAssignments.Add(assignment);
            await _context.SaveChangesAsync(ct);

            return new ScanContainerResult(assignment.Id, container.Id, container.Code, (int)container.Type);
        }
    }

    // ── Manuel container boşaltma (yönetici, sebep zorunlu) ──────────────────
    public record ReleaseContainerCommand(int ContainerAssignmentId, string Reason) : IRequest<Unit>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager" };
    }

    public class ReleaseContainerCommandHandler : IRequestHandler<ReleaseContainerCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public ReleaseContainerCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Unit> Handle(ReleaseContainerCommand request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
                throw new DomainException("Boşaltma sebebi zorunludur.");

            var a = await _context.ContainerAssignments
                .FirstOrDefaultAsync(x => x.Id == request.ContainerAssignmentId, ct)
                ?? throw new NotFoundException("ContainerAssignment", request.ContainerAssignmentId);

            if (a.ReleasedAt != null)
                return Unit.Value; // zaten boşaltılmış (idempotent)

            a.ReleasedAt = DateTime.UtcNow;
            a.ReleaseReason = request.Reason.Trim();
            await _context.SaveChangesAsync(ct);
            return Unit.Value;
        }
    }
}
