using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.DeleteDraftShipment
{
    public record DeleteDraftShipmentCommand(int ShipmentId) : IRequest<Unit>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting" };
    }

    public class DeleteDraftShipmentCommandHandler : IRequestHandler<DeleteDraftShipmentCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public DeleteDraftShipmentCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Unit> Handle(DeleteDraftShipmentCommand request, CancellationToken cancellationToken)
        {
            var shipment = await _context.Shipments
                .FirstOrDefaultAsync(s => s.Id == request.ShipmentId, cancellationToken)
                ?? throw new NotFoundException("Shipment", request.ShipmentId);

            if (shipment.Status != ShipmentStatus.Created)
                throw new DomainException(
                    $"Yalnızca taslak (Oluşturuldu) durumundaki sevkiyatlar silinebilir. Mevcut durum: {shipment.Status}.");

            // Mark cancelled so it disappears from all views
            shipment.ChangeStatus(ShipmentStatus.Cancelled, _currentUser.UserId, "Taslak sevkiyat silindi.");

            // Release the ISS order so it reappears in the import list
            var issOrder = await _context.IssOrders
                .FirstOrDefaultAsync(o => o.Id == shipment.IssOrderId, cancellationToken);

            if (issOrder != null)
                issOrder.IsTransferred = false;

            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
