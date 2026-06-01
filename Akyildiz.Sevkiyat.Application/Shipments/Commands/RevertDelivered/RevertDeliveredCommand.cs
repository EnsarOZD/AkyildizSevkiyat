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
        private readonly IPhotoStorageService _photos;

        public RevertDeliveredCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IPhotoStorageService photos)
        {
            _context = context;
            _currentUserService = currentUserService;
            _photos = photos;
        }

        public async Task Handle(RevertDeliveredCommand request, CancellationToken cancellationToken)
        {
            var shipment = await _context.Shipments
                .FirstOrDefaultAsync(s => s.Id == request.ShipmentId, cancellationToken)
                ?? throw new NotFoundException("Shipment", request.ShipmentId);

            if (shipment.Status != ShipmentStatus.Delivered && shipment.Status != ShipmentStatus.Dispatched)
                throw new DomainException("Yalnızca 'Teslim Edildi' veya 'Gönderildi' durumundaki sevkiyatlar geri alınabilir.");

            List<string> photoPathsToDelete = new();
            if (shipment.Status == ShipmentStatus.Delivered)
            {
                shipment.ClearDeliveryProof();

                var photoRows = await _context.ShipmentDeliveryPhotos
                    .Where(p => p.ShipmentId == shipment.Id)
                    .ToListAsync(cancellationToken);
                if (photoRows.Count > 0)
                {
                    photoPathsToDelete = photoRows.Select(p => p.PhotoPath).ToList();
                    _context.ShipmentDeliveryPhotos.RemoveRange(photoRows);
                }
            }

            shipment.ChangeStatus(
                ShipmentStatus.ReadyForDispatch,
                _currentUserService.UserId,
                $"Admin geri al — {request.Reason}");

            await _context.SaveChangesAsync(cancellationToken);

            // Best-effort disk cleanup after DB commit
            foreach (var path in photoPathsToDelete)
                await _photos.DeleteAsync(path);
        }
    }
}
