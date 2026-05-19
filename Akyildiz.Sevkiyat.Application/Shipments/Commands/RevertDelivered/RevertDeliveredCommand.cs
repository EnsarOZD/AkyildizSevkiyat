using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.RevertDelivered
{
    public record RevertDeliveredCommand(int ShipmentId, string Reason) : IRequest, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin" };
    }

    public class RevertDeliveredCommandHandler : IRequestHandler<RevertDeliveredCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public RevertDeliveredCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task Handle(RevertDeliveredCommand request, CancellationToken cancellationToken)
        {
            var shipment = await _context.Shipments
                .FirstOrDefaultAsync(s => s.Id == request.ShipmentId, cancellationToken)
                ?? throw new NotFoundException("Shipment", request.ShipmentId);

            if (shipment.Status != ShipmentStatus.Delivered && shipment.Status != ShipmentStatus.Dispatched)
                throw new DomainException("Yalnızca 'Teslim Edildi' veya 'Gönderildi' durumundaki sevkiyatlar geri alınabilir.");

            if (shipment.Status == ShipmentStatus.Delivered)
                shipment.ClearDeliveryProof();

            shipment.ChangeStatus(
                ShipmentStatus.ReadyForDispatch,
                _currentUserService.UserId,
                $"Admin geri al — {request.Reason}");

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
