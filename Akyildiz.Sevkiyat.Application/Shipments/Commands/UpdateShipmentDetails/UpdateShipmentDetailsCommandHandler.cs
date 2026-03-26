using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.UpdateShipmentDetails
{
    public class UpdateShipmentDetailsCommandHandler : IRequestHandler<UpdateShipmentDetailsCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public UpdateShipmentDetailsCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Unit> Handle(UpdateShipmentDetailsCommand request, CancellationToken cancellationToken)
        {
            // 2. Fetch Shipment
            var shipment = await _context.Shipments
                .Include(s => s.Lines)
                .FirstOrDefaultAsync(s => s.Id == request.ShipmentId, cancellationToken);
            
            if (shipment == null)
                throw new NotFoundException("Shipment", request.ShipmentId);

            // 3. Status Check (Critical)
            if (shipment.Status != ShipmentStatus.Created)
            {
                throw new DomainException("Cannot edit shipment. Only 'Created' (Draft) shipments can be edited.");
            }

            // 4. Update Header
            // Using domain method ensures encapsulation if we add more logic there later
            shipment.UpdateDeliveryDate(request.DeliveryDate);
            
            // 5. Update Lines
            // Strategy: 
            // - Updates existing lines
            // - Removes lines not in the list? (User requirement: 'Stock Card swap' -> implies changing a line or adding/removing)
            // - Adds new lines?
            // Safer approach for 'Sync':
            // If LineId matches, update.
            // If LineId is null/0, add.
            // What about deletions? Logic: If a line in DB is NOT in request, delete it?
            // Let's assume the frontend sends the COMPLETE list of desired lines.
            
            var requestLineIds = request.Lines.Where(l => l.LineId.HasValue && l.LineId.Value > 0).Select(l => l.LineId!.Value).ToList();
            var dbLineIds = shipment.Lines.Select(l => l.Id).ToList();

            // Detect Deletions
            var linesToDelete = shipment.Lines.Where(l => !requestLineIds.Contains(l.Id)).ToList();
            foreach(var lineToDelete in linesToDelete)
            {
                _context.ShipmentLines.Remove(lineToDelete);
                // shipment.Lines.Remove(lineToDelete); // EF Core tracking handles this but good to be explicit if needed
            }

            // Detect Updates & Inserts
            foreach (var lineDto in request.Lines)
            {
                if (lineDto.LineId.HasValue && lineDto.LineId.Value > 0)
                {
                    // Update
                    var dbLine = shipment.Lines.FirstOrDefault(l => l.Id == lineDto.LineId.Value);
                    if (dbLine != null)
                    {
                        dbLine.UpdateStockInfo(lineDto.StockCode, lineDto.StockName, lineDto.Unit);
                        dbLine.UpdateOrderedQty(lineDto.OrderedQty);
                    }
                }
                else
                {
                    // Insert
                    var newLine = ShipmentLine.Create(null, null, lineDto.StockCode, lineDto.StockName, lineDto.Unit, lineDto.OrderedQty);
                    _context.ShipmentLines.Add(newLine);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
