using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.ClothingPicking
{
    public record ClothingPickLineInput(int ShipmentLineId, decimal DeliveredQty, string? DifferenceReason, string? Note);

    // ── Toplama ilerlemesi kaydet (ara kayıt; sebep zorunlu DEĞİL) ──────────
    public record SaveClothingPickProgressCommand(int ShipmentId, List<ClothingPickLineInput> Lines)
        : IRequest<Unit>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager", "Accounting", "Warehouse" };
    }

    public class SaveClothingPickProgressCommandHandler : IRequestHandler<SaveClothingPickProgressCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        public SaveClothingPickProgressCommandHandler(IApplicationDbContext c, ICurrentUserService u) { _context = c; _currentUser = u; }

        public async Task<Unit> Handle(SaveClothingPickProgressCommand request, CancellationToken ct)
        {
            var s = await _context.Shipments.Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == request.ShipmentId, ct)
                ?? throw new NotFoundException("Shipment", request.ShipmentId);

            ClothingPickingGuards.EnsureOwnerOrManager(s, _currentUser);
            if (s.Status != ShipmentStatus.Picking)
                throw new DomainException("Toplama yalnızca 'Hazırlanıyor' durumunda kaydedilebilir.");

            bool hasOpenContainer = await _context.ContainerAssignments
                .AnyAsync(a => a.ShipmentId == s.Id && a.ReleasedAt == null, ct);
            ClothingPickingGuards.EnsurePickingReady(s, hasOpenContainer);

            var map = s.Lines.ToDictionary(l => l.Id);
            foreach (var dto in request.Lines)
            {
                if (!map.TryGetValue(dto.ShipmentLineId, out var line)) continue;
                line.SavePickingProgress(dto.DeliveredQty, dto.DifferenceReason); // ara kayıt — sebep zorunlu değil
            }

            await _context.SaveChangesAsync(ct);
            return Unit.Value;
        }
    }

    // ── "Toplama bitti" ─────────────────────────────────────────────────────
    public record CompletePickingCommand(
        int ShipmentId,
        List<ClothingPickLineInput> Lines,
        bool ConfirmContainers,   // Cart modunda bağlı arabaların onayı
        int? PalletCount          // Pallet modunda zorunlu
    ) : IRequest<Unit>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager", "Accounting", "Warehouse" };
    }

    public class CompletePickingCommandHandler : IRequestHandler<CompletePickingCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        public CompletePickingCommandHandler(IApplicationDbContext c, ICurrentUserService u) { _context = c; _currentUser = u; }

        public async Task<Unit> Handle(CompletePickingCommand request, CancellationToken ct)
        {
            var s = await _context.Shipments.Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == request.ShipmentId, ct)
                ?? throw new NotFoundException("Shipment", request.ShipmentId);

            ClothingPickingGuards.EnsureOwnerOrManager(s, _currentUser);
            if (s.Status != ShipmentStatus.Picking)
                throw new DomainException("Toplama yalnızca 'Hazırlanıyor' durumunda bitirilebilir.");

            bool hasOpenContainer = await _context.ContainerAssignments
                .AnyAsync(a => a.ShipmentId == s.Id && a.ReleasedAt == null, ct);
            ClothingPickingGuards.EnsurePickingReady(s, hasOpenContainer);

            // Mod'a özgü zorunluluklar
            if (s.PickingMode == PickingMode.Cart && !request.ConfirmContainers)
                throw new DomainException("Bağlı arabaların onaylanması gerekir.");
            if (s.PickingMode == PickingMode.Pallet)
            {
                if (!request.PalletCount.HasValue || request.PalletCount.Value <= 0)
                    throw new DomainException("Palet modunda palet sayısı (PalletCount) zorunludur.");
                s.PalletCount = request.PalletCount.Value;
            }

            // Final miktarlar — SetDeliveredQty eksik/fark durumunda sebep zorunlu kılar
            var map = s.Lines.ToDictionary(l => l.Id);
            foreach (var dto in request.Lines)
            {
                if (!map.TryGetValue(dto.ShipmentLineId, out var line)) continue;
                line.SetDeliveredQty(dto.DeliveredQty, dto.DifferenceReason, dto.Note);
            }

            // Toplama bitti — DURUM DEĞİŞMEZ (kapama ayrı adımda)
            s.MarkPickingCompleted(_currentUser.FullName ?? _currentUser.Email ?? "Bilinmiyor");

            await _context.SaveChangesAsync(ct);
            return Unit.Value;
        }
    }
}
