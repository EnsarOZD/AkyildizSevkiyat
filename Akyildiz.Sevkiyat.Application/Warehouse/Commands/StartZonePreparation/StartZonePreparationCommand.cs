using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.StartZonePreparation
{
    public record StartZonePreparationCommand(int ZonePreparationId) : IRequest<bool>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Warehouse", "Dispatcher" };
    }

    public class StartZonePreparationCommandHandler : IRequestHandler<StartZonePreparationCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public StartZonePreparationCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<bool> Handle(StartZonePreparationCommand request, CancellationToken cancellationToken)
        {
            var zp = await _context.ZonePreparations
                .FirstOrDefaultAsync(z => z.Id == request.ZonePreparationId, cancellationToken);

            if (zp == null) throw new NotFoundException("ZonePreparation", request.ZonePreparationId);

            // Guard Rule: Only Draft can start
            if (zp.IsFrozen)
                throw new DomainException("Bu bölge için hazırlık zaten dondurulmuş (başlatılmış).");

            if (zp.Status != ZonePreparationStatus.Draft)
                throw new DomainException($"Hazırlık başlatılamaz çünkü şu anki durumu: {zp.Status}");

            // Guard: Kıyafet operasyonu için bu akış henüz desteklenmiyor.
            // TODO: CLOTHING_PICKING — Kıyafet picking akışı tasarlandığında bu guard kaldırılacak.
            var hasClothingShipments = await _context.Shipments
                .AnyAsync(s => s.ZonePreparationId == zp.Id
                            && s.OperationType == OperationType.Clothing,
                          cancellationToken);

            if (hasClothingShipments)
                throw new DomainException(
                    "Kıyafet operasyonu sevkiyatları için zone bazlı picking henüz desteklenmiyor. " +
                    "Bu özellik yakında eklenecektir.");

            // Apply Freeze
            zp.Status = ZonePreparationStatus.MicroPicking;
            zp.IsFrozen = true;
            zp.StartedAt = DateTime.UtcNow;
            
            // Set User
            // Set User
            if (_currentUserService.UserId.HasValue)
            {
                zp.StartedByUserId = _currentUserService.UserId.Value;
            }

            // Implicitly, all linked shipments are now "Frozen" in this batch.
            // Since we assign ZonePreparationId to Shipments in the Dashboard/Query phase, 
            // any shipment currently linked to this ZP is now part of the frozen payload.
            // New shipments will not find this ZP eligible (because IsFrozen=true) 
            // and will force creation of a new Batch.

            // Sync Shipment Status for this Batch
            // Update Shipments linked to this ZonePreparation to 'Picking' if they are 'AssignedToWarehouse'
            
            // Note: Since we haven't updated Shipments locally in this command, we assume they are already linked.
            // However, to be safe, we should ensure all shipments that SHOULD be in this batch ARE linked.
            // But the Query logic handles assignment. 
            // So if User sees it on Dashboard, it's assigned.
            
            // Status Update: MicroPicking involves changing shipments to Picking?
            // "Zone MicroPicking → Shipment Picking"
            
            var shipments = await _context.Shipments
                .Where(s => s.ZonePreparationId == zp.Id 
                            && s.Status == ShipmentStatus.AssignedToWarehouse)
                .ToListAsync(cancellationToken);

            foreach (var s in shipments)
            {
                s.ChangeStatus(ShipmentStatus.Picking, zp.StartedByUserId);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
