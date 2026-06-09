using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.ClothingPicking
{
    /// <summary>
    /// İşi bırakma. Picking → Created (revert, sebep zorunlu). Toplanan miktar (DeliveredQty)
    /// SIFIRLANMAZ — kısmi toplama korunur. Kullanıcı yalnızca KENDİ claim'ini bırakabilir;
    /// başkasının claim'ini yalnızca Admin/Manager kaldırabilir.
    /// </summary>
    public record UnclaimShipmentCommand(int ShipmentId, string Reason) : IRequest<Unit>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager", "Accounting", "Warehouse" };
    }

    public class UnclaimShipmentCommandValidator : AbstractValidator<UnclaimShipmentCommand>
    {
        public UnclaimShipmentCommandValidator()
        {
            RuleFor(x => x.Reason).NotEmpty().WithMessage("Bırakma sebebi zorunludur.").MaximumLength(500);
        }
    }

    public class UnclaimShipmentCommandHandler : IRequestHandler<UnclaimShipmentCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UnclaimShipmentCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Unit> Handle(UnclaimShipmentCommand request, CancellationToken ct)
        {
            var s = await _context.Shipments
                .FirstOrDefaultAsync(x => x.Id == request.ShipmentId, ct)
                ?? throw new NotFoundException("Shipment", request.ShipmentId);

            if (s.OperationType != OperationType.Clothing)
                throw new DomainException("Bu işlem yalnızca kıyafet sevkiyatları içindir.");

            if (s.Status != ShipmentStatus.Picking || s.AssignedPickerId == null)
                throw new DomainException("Yalnızca üstlenilmiş (hazırlanıyor) sevkiyatlar bırakılabilir.");

            // Yetki: kendi claim'i ya da yönetici
            bool isOwner = s.AssignedPickerId == _currentUser.UserId;
            bool isManager = _currentUser.Role is UserRole.Admin or UserRole.Manager;
            if (!isOwner && !isManager)
                throw new ForbiddenException("Yalnızca işi alan toplayıcı veya yönetici bırakabilir.");

            // Claim sahipliğini temizle (kısmi toplama / DeliveredQty KORUNUR)
            s.AssignedPickerId = null;
            s.AssignedPickerName = null;
            s.ClaimedAt = null;
            s.ClaimedOutOfOrder = false;
            s.PickingPausedAt = null;

            // Picking → Created (revert; ChangeStatus sebebi zorunlu kılar)
            s.ChangeStatus(ShipmentStatus.Created, _currentUser.UserId, $"İş bırakıldı: {request.Reason}");

            await _context.SaveChangesAsync(ct);
            return Unit.Value;
        }
    }
}
