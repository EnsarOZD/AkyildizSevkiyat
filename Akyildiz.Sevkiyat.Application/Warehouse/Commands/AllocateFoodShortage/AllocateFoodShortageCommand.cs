using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.AllocateFoodShortage
{
    public class FoodShortageAllocationDto
    {
        public int ShipmentLineId { get; set; }
        public decimal DeliveredQty { get; set; }
    }

    public class AllocateFoodShortageCommand : IRequest<bool>, IRequireRoles
    {
        public List<FoodShortageAllocationDto> Allocations { get; set; } = new();
        public string? DifferenceReason { get; set; }

        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting", "Warehouse" };
    }

    public class AllocateFoodShortageCommandHandler : IRequestHandler<AllocateFoodShortageCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public AllocateFoodShortageCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(AllocateFoodShortageCommand request, CancellationToken cancellationToken)
        {
            if (!request.Allocations.Any()) return false;

            var ids = request.Allocations.Select(a => a.ShipmentLineId).ToList();

            var lines = await _context.ShipmentLines
                .Include(sl => sl.Shipment)
                .Where(sl => ids.Contains(sl.Id))
                .ToListAsync(cancellationToken);

            if (lines.Count != ids.Count)
                throw new NotFoundException("Bazı satırlar bulunamadı.");

            // Satırların bağlı olduğu zone prep'leri tek sorguda çek ve doğrula
            var zonePrepIds = lines
                .Select(l => l.Shipment.ZonePreparationId)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            var zonePreps = await _context.ZonePreparations
                .Where(zp => zonePrepIds.Contains(zp.Id))
                .ToListAsync(cancellationToken);

            foreach (var zp in zonePreps)
            {
                if (!zp.IsFrozen)
                    throw new DomainException("Hazırlık başlatılmadan miktar girişi yapılamaz.");
                if (zp.Status >= ZonePreparationStatus.ReadyForTransfer)
                    throw new DomainException("Sevkiyat araca atandıktan sonra miktar değiştirilemez.");
            }

            foreach (var alloc in request.Allocations)
            {
                var line = lines.First(l => l.Id == alloc.ShipmentLineId);
                if (line.DeliveredQty == alloc.DeliveredQty) continue;

                line.SetDeliveredQty(alloc.DeliveredQty,
                    request.DifferenceReason ?? "Gıda Eksik Dağıtım",
                    "Kullanıcı Gıda Eksik Dağıtımı Yaptı");

                _context.ShipmentHistories.Add(new Domain.Entities.ShipmentHistory
                {
                    ShipmentId  = line.ShipmentId,
                    OldStatus   = line.Shipment.Status,
                    NewStatus   = line.Shipment.Status,
                    ChangedAt   = DateTime.UtcNow,
                    Description = $"Gıda Eksik Dağıtım: {alloc.DeliveredQty} adet (Sipariş: {line.OrderedQty})"
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
