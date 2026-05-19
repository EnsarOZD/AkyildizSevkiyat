using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Warehouse.Services;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using System.Collections.Generic;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.MarkWarehousePickup
{
    public sealed record MarkWarehousePickupCommand(
        int ShipmentId,
        string RecipientName,
        string? Note = null
    ) : IRequest, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager", "Accounting", "Warehouse", "Dispatcher" };
    }

    public sealed class MarkWarehousePickupCommandHandler : IRequestHandler<MarkWarehousePickupCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ZoneAutoCloseService _zoneAutoClose;

        public MarkWarehousePickupCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            ZoneAutoCloseService zoneAutoClose)
        {
            _context = context;
            _currentUserService = currentUserService;
            _zoneAutoClose = zoneAutoClose;
        }

        public async Task Handle(MarkWarehousePickupCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.RecipientName))
                throw new DomainException("Teslim alan kişi adı zorunludur.");

            var shipment = await _context.Shipments
                .Include(s => s.Lines)
                .FirstOrDefaultAsync(s => s.Id == request.ShipmentId, cancellationToken)
                ?? throw new NotFoundException("Shipment", request.ShipmentId);

            if (shipment.Status == ShipmentStatus.Delivered)
                return;

            if (shipment.Status != ShipmentStatus.ReadyForDispatch)
                throw new DomainException("Depo teslim yalnızca 'Sevkiyata Hazır' durumundaki sevkiyatlar için yapılabilir.");

            shipment.RecordDelivery(
                DateTime.UtcNow,
                request.RecipientName,
                request.Note,
                null,
                _currentUserService.UserId,
                _currentUserService.Role?.ToString(),
                "Depo teslim — müşteri depodan teslim aldı.",
                null, null, null);

            shipment.ChangeStatus(ShipmentStatus.Delivered, _currentUserService.UserId, "Depo teslim — müşteri depodan teslim aldı.");

            // Stok çıkışı
            var mappedLineIds = shipment.Lines
                .Where(l => l.StockMasterId.HasValue)
                .Select(l => l.StockMasterId!.Value)
                .Distinct()
                .ToList();

            if (mappedLineIds.Count > 0)
            {
                var stocks = await _context.StockMasters
                    .Where(s => mappedLineIds.Contains(s.Id))
                    .ToListAsync(cancellationToken);

                var stockMap = stocks.ToDictionary(s => s.Id);

                foreach (var line in shipment.Lines.Where(l => l.StockMasterId.HasValue))
                {
                    if (!stockMap.TryGetValue(line.StockMasterId!.Value, out var stock)) continue;
                    var qty = line.DeliveredQty > 0 ? line.DeliveredQty : line.OrderedQty;
                    stock.Deduct(qty);

                    _context.StockTransactions.Add(new StockTransaction
                    {
                        StockMasterId = stock.Id,
                        Type          = StockTransactionType.ShipmentOut,
                        Qty           = -qty,
                        Reference     = $"SHP-{shipment.Id}",
                        Date          = DateTime.UtcNow,
                        Note          = $"Depo teslim — Sevkiyat #{shipment.Id}"
                    });
                }
            }

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException("Stok, aynı anda başka bir işlem tarafından güncellendi. Lütfen tekrar deneyin.");
            }

            // Tüm projeler teslim edildiyse zonu otomatik kapat
            if (shipment.ZonePreparationId.HasValue)
            {
                await _zoneAutoClose.TryAutoCloseAsync(shipment.ZonePreparationId.Value, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
