using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.WarehouseLocations.Commands.UpdateWarehouseLocation
{
    public record UpdateWarehouseLocationCommand(
        int           Id,
        LocationType  LocationType,
        string?       Description,
        decimal?      MaxWeightKg,
        int?          MaxPallets,
        bool          IsActive,
        string?       Alan,
        string?       QrCode,
        int?          TotalFloors,
        ContainerType ContainerType,
        string?       InnerLevel,
        int?          InnerPosition
    ) : IRequest;

    public class UpdateWarehouseLocationCommandHandler : IRequestHandler<UpdateWarehouseLocationCommand>
    {
        private readonly IApplicationDbContext _context;

        public UpdateWarehouseLocationCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(UpdateWarehouseLocationCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.WarehouseLocations
                .FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken)
                ?? throw new NotFoundException("Lokasyon bulunamadı.");

            entity.LocationType   = request.LocationType;
            entity.Description    = request.Description?.Trim();
            entity.MaxWeightKg    = request.MaxWeightKg;
            entity.MaxPallets     = request.MaxPallets;
            entity.IsActive       = request.IsActive;
            entity.Alan           = request.Alan?.Trim();
            entity.QrCode         = request.QrCode?.Trim();
            entity.TotalFloors    = request.TotalFloors;
            entity.ContainerType  = request.ContainerType;
            entity.InnerLevel     = request.InnerLevel?.Trim().ToUpperInvariant();
            entity.InnerPosition  = request.InnerPosition;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
