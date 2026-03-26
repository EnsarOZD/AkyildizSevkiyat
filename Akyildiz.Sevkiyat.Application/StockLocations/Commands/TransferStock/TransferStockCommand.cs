using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.StockLocations.Commands.TransferStock
{
    /// <summary>
    /// Bir stok kaleminin belirtilen miktarını kaynak lokasyondan hedef lokasyona taşır.
    /// StockMaster.OnHandQty değişmez — yalnızca lokasyon dağılımı güncellenir.
    /// </summary>
    public record TransferStockCommand(
        int StockMasterId,
        int FromLocationId,
        int ToLocationId,
        decimal Qty,
        string? Note,
        int? TransferredByUserId
    ) : IRequest<int>;

    public class TransferStockCommandHandler
        : IRequestHandler<TransferStockCommand, int>
    {
        private readonly IApplicationDbContext _context;

        public TransferStockCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(
            TransferStockCommand request,
            CancellationToken cancellationToken)
        {
            if (request.Qty <= 0)
                throw new DomainException("Transfer miktarı sıfırdan büyük olmalıdır.");

            if (request.FromLocationId == request.ToLocationId)
                throw new DomainException("Kaynak ve hedef lokasyon aynı olamaz.");

            // Source
            var source = await _context.StockLocations
                .FirstOrDefaultAsync(sl =>
                    sl.StockMasterId == request.StockMasterId &&
                    sl.WarehouseLocationId == request.FromLocationId,
                    cancellationToken)
                ?? throw new NotFoundException("Kaynak lokasyonda bu stok bulunamadı.");

            if (source.AvailableQty < request.Qty)
                throw new DomainException(
                    $"Kaynak lokasyonda yeterli stok yok. Mevcut: {source.AvailableQty}, İstenen: {request.Qty}");

            // Validate target location exists
            var targetLocExists = await _context.WarehouseLocations
                .AnyAsync(l => l.Id == request.ToLocationId && l.IsActive, cancellationToken);
            if (!targetLocExists)
                throw new NotFoundException("Hedef lokasyon bulunamadı.");

            // Deduct from source
            source.OnHandQty    -= request.Qty;
            source.LastMovedAt   = DateTime.UtcNow;

            // Add to target (upsert)
            var target = await _context.StockLocations
                .FirstOrDefaultAsync(sl =>
                    sl.StockMasterId == request.StockMasterId &&
                    sl.WarehouseLocationId == request.ToLocationId,
                    cancellationToken);

            if (target == null)
            {
                target = new StockLocation
                {
                    StockMasterId       = request.StockMasterId,
                    WarehouseLocationId = request.ToLocationId,
                    OnHandQty           = 0,
                };
                _context.StockLocations.Add(target);
            }

            target.OnHandQty  += request.Qty;
            target.LastMovedAt = DateTime.UtcNow;

            // Transfer log
            var transfer = new LocationTransfer
            {
                StockMasterId       = request.StockMasterId,
                FromLocationId      = request.FromLocationId,
                ToLocationId        = request.ToLocationId,
                Qty                 = request.Qty,
                Note                = request.Note?.Trim(),
                TransferredByUserId = request.TransferredByUserId,
                TransferredAt       = DateTime.UtcNow,
            };
            _context.LocationTransfers.Add(transfer);

            await _context.SaveChangesAsync(cancellationToken);
            return transfer.Id;
        }
    }
}
