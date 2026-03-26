using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.AllocateMacroShortage
{
    public class ShortageAllocationDto
    {
        public int ShipmentLineId { get; set; }
        public decimal DeliveredQty { get; set; }
    }

    public class AllocateMacroShortageCommand : IRequest<bool>
    {
        public int ZonePreparationId { get; set; }
        public List<ShortageAllocationDto> Allocations { get; set; } = new();
    }

    public class AllocateMacroShortageCommandHandler : IRequestHandler<AllocateMacroShortageCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public AllocateMacroShortageCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(AllocateMacroShortageCommand request, CancellationToken cancellationToken)
        {
            if (!request.Allocations.Any()) return false;

             // 1. Validate Zone Prep
            var zp = await _context.ZonePreparations.FindAsync(new object[] { request.ZonePreparationId }, cancellationToken);
            if (zp == null) throw new NotFoundException("ZonePreparation", request.ZonePreparationId);

             if (!zp.IsFrozen)
                throw new DomainException("Hazırlık başlatılmadan miktar girişi yapılamaz.");

             if (zp.Status >= ZonePreparationStatus.ReadyForTransfer)
                throw new DomainException("Sevkiyat araca atandıktan sonra miktar değiştirilemez.");

            var ids = request.Allocations.Select(a => a.ShipmentLineId).ToList();
            var lines = await _context.ShipmentLines
                .Include(sl => sl.Shipment)
                .Where(sl => ids.Contains(sl.Id))
                .ToListAsync(cancellationToken);

            // Guard: All lines must exist and belong
            if (lines.Count != ids.Count) throw new NotFoundException("Bazı satırlar bulunamadı.");
            if (lines.Any(l => l.Shipment.ZonePreparationId != request.ZonePreparationId)) 
                throw new DomainException("Yabancı satır tespit edildi.");

            foreach(var alloc in request.Allocations)
            {
                var line = lines.First(l => l.Id == alloc.ShipmentLineId);
                
                // Guard: cannot exceed Ordered
                // Guard check removed to allow over-picking per user request

                if (line.DeliveredQty != alloc.DeliveredQty)
                {
                    line.SetDeliveredQty(alloc.DeliveredQty, "MacroAllocation", "Kullanıcı Eksik Dağıtımı Yaptı");
                    
                     _context.ShipmentHistories.Add(new Domain.Entities.ShipmentHistory {
                        ShipmentId = line.ShipmentId,
                        OldStatus = line.Shipment.Status,
                        NewStatus = line.Shipment.Status,
                        ChangedAt = DateTime.UtcNow,
                        Description = $"Eksik Dağıtımı: {alloc.DeliveredQty} (Sipariş: {line.OrderedQty})"
                    });
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
