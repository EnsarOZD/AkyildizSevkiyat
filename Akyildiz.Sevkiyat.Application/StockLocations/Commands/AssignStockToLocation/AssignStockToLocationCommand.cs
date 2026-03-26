using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.StockLocations.Commands.AssignStockToLocation
{
    /// <summary>
    /// Bir stok kalemini bir lokasyona atar (ilk yerleştirme / manuel atama).
    /// Stok miktarı değişmez; sadece "bu stok bu lokasyonda" kaydı oluşur.
    /// Zaten bu kombinasyon varsa günceller.
    /// </summary>
    public record AssignStockToLocationCommand(
        int StockMasterId,
        int WarehouseLocationId,
        decimal Qty
    ) : IRequest<int>;

    public class AssignStockToLocationCommandHandler
        : IRequestHandler<AssignStockToLocationCommand, int>
    {
        private readonly IApplicationDbContext _context;

        public AssignStockToLocationCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(
            AssignStockToLocationCommand request,
            CancellationToken cancellationToken)
        {
            // Validate references
            var stockExists = await _context.StockMasters
                .AnyAsync(s => s.Id == request.StockMasterId && s.IsActive, cancellationToken);
            if (!stockExists)
                throw new NotFoundException("Stok bulunamadı.");

            var locExists = await _context.WarehouseLocations
                .AnyAsync(l => l.Id == request.WarehouseLocationId && l.IsActive, cancellationToken);
            if (!locExists)
                throw new NotFoundException("Lokasyon bulunamadı.");

            if (request.Qty < 0)
                throw new DomainException("Miktar sıfırdan küçük olamaz.");

            var existing = await _context.StockLocations
                .FirstOrDefaultAsync(sl =>
                    sl.StockMasterId == request.StockMasterId &&
                    sl.WarehouseLocationId == request.WarehouseLocationId,
                    cancellationToken);

            if (existing != null)
            {
                existing.OnHandQty = request.Qty;
                existing.LastMovedAt = DateTime.UtcNow;
            }
            else
            {
                existing = new StockLocation
                {
                    StockMasterId       = request.StockMasterId,
                    WarehouseLocationId = request.WarehouseLocationId,
                    OnHandQty           = request.Qty,
                    LastMovedAt         = DateTime.UtcNow,
                };
                _context.StockLocations.Add(existing);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return existing.Id;
        }
    }
}
