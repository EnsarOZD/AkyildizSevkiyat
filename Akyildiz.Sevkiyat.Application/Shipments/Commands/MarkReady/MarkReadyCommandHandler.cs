using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.Reconciliation.Services;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.MarkReady
{
    public class MarkReadyCommandHandler : IRequestHandler<MarkReadyCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ReconciliationGuard _guard;

        public MarkReadyCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            ReconciliationGuard guard)
        {
            _context = context;
            _currentUserService = currentUserService;
            _guard = guard;
        }

        public async Task<Unit> Handle(MarkReadyCommand request, CancellationToken cancellationToken)
        {
            // ── Enforcement: ISS miktar uyumsuzluğu varsa hazır işaretlenemez ──
            await _guard.ThrowIfIssQtyMismatchAsync(request.ShipmentId, cancellationToken);

            var shipment = await _context.Shipments
                .FirstOrDefaultAsync(x => x.Id == request.ShipmentId, cancellationToken);

            if (shipment == null)
            {
                throw new NotFoundException("Shipment", request.ShipmentId);
            }

            shipment.ChangeStatus(ShipmentStatus.ReadyForDispatch, _currentUserService.UserId, request.Reason);

            await _context.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
