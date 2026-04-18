using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.UpdateShipmentQuantities
{
    public record ShipmentLineUpdateDto(int LineId, decimal DeliveredQty, string? DifferenceReason, string? Note);

    public record UpdateShipmentQuantitiesCommand(int ShipmentId, List<ShipmentLineUpdateDto> Lines) : IRequest<Unit>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting", "Warehouse" };
    }

    public class UpdateShipmentQuantitiesCommandHandler : IRequestHandler<UpdateShipmentQuantitiesCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public UpdateShipmentQuantitiesCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Unit> Handle(UpdateShipmentQuantitiesCommand request, CancellationToken cancellationToken)
        {
            // 2. Shipment'ı çek
            var shipment = await _context.Shipments
                .Include(s => s.Lines)
                .FirstOrDefaultAsync(s => s.Id == request.ShipmentId, cancellationToken);
 
            if (shipment == null)
                throw new NotFoundException("Shipment", request.ShipmentId);

            // 3. Durum Kontrolü
            // Sadece Picking veya AssignedToWarehouse aşamasında düzenlenebilir.
            // ReadyForDispatch olduktan sonra genelde değişmez ama esneklik için Picking ve sonrası diyelim mi?
            // "Picking" aşaması en doğrusu.
            if (shipment.Status != ShipmentStatus.Picking && shipment.Status != ShipmentStatus.AssignedToWarehouse)
            {
                 // Esneklik: Picking işlemine başlamadan (Assigned) veya başladığında (Picking) güncelleyebilsin.
                 // Ancak MarkReady yapıldıysa (ReadyForDispatch) geri alması gerekir.
                 // Şimdilik sadece Picking'de izin verelim, akışı zorlayalım.
                 if(shipment.Status != ShipmentStatus.Picking)
                    throw new DomainException($"Shipment must be in 'Picking' status to update quantities. Current status: {shipment.Status}");
            }

            // 4. Satırları güncelle
            foreach (var updateDto in request.Lines)
            {
                var line = shipment.Lines.FirstOrDefault(l => l.Id == updateDto.LineId);
                if (line == null) continue; // veya hata fırlat

                // Domain metodunu çağır (Validation logic orada)
                line.SetDeliveredQty(updateDto.DeliveredQty, updateDto.DifferenceReason, updateDto.Note);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
