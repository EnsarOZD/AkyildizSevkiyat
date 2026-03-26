using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.Reconciliation.Services;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.MarkShipmentDelivered
{
    public sealed record MarkShipmentDeliveredCommand(
        int ShipmentId,
        string? DeliveryNote = null,
        string? DeliveryRecipient = null,
        string? DeliveryPhotoBase64 = null,
        string? OverrideNote = null
    ) : IRequest;

    public sealed class MarkShipmentDeliveredCommandHandler
        : IRequestHandler<MarkShipmentDeliveredCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ReconciliationGuard _guard;

        public MarkShipmentDeliveredCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            ReconciliationGuard guard)
        {
            _context = context;
            _currentUserService = currentUserService;
            _guard = guard;
        }

        public async Task Handle(MarkShipmentDeliveredCommand request, CancellationToken cancellationToken)
        {
            // ── Enforcement: Picking tamamlanmadan teslim edilemez ──────────────
            await _guard.ThrowIfPickingIncompleteAsync(request.ShipmentId, cancellationToken);

            var shipment = await _context.Shipments
                .Include(s => s.Lines)
                .FirstOrDefaultAsync(s => s.Id == request.ShipmentId, cancellationToken);

            if (shipment is null)
                throw new NotFoundException("Shipment", request.ShipmentId);

            // 1. FIX — Double Delivery (CRITICAL) - Idempotency guard
            if (shipment.Status == ShipmentStatus.Delivered)
            {
                return;
            }

            // 2. FIX — Explicit Status Guard
            if (shipment.Status != ShipmentStatus.Dispatched)
            {
                throw new DomainException("Only dispatched shipments can be delivered.");
            }

            // 5. FIX — Delivery Proof (MINIMUM)
            if (string.IsNullOrWhiteSpace(request.DeliveryRecipient))
            {
                throw new DomainException("Delivery recipient is required.");
            }

            // 6. FIX - Driver Ownership & Override
            bool isOverride = false;
            string? overrideNoteToSave = null;

            if (_currentUserService.Role == UserRole.Driver)
            {
                var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == _currentUserService.UserId, cancellationToken)
                    ?? throw new ForbiddenException("Kullanıcıya tanımlı bir şoför kaydı bulunamadı.");

                if (shipment.AssignedDriverId != driver.Id)
                {
                    throw new ForbiddenException("You are not assigned to this shipment.");
                }
            }
            else
            {
                // Dispatcher/Admin processing shipment -> always an override
                isOverride = true;
                if (string.IsNullOrWhiteSpace(request.OverrideNote))
                {
                    throw new DomainException("Yönetici/Operasyon işlemi için Override Notu (Açıklama) girmek zorunludur.");
                }
                overrideNoteToSave = request.OverrideNote;
            }

            shipment.RecordDelivery(
                DateTime.UtcNow,
                request.DeliveryRecipient!,
                request.DeliveryNote,
                request.DeliveryPhotoBase64,
                _currentUserService.UserId,
                _currentUserService.Role?.ToString(),
                overrideNoteToSave);

            string? statusReason = isOverride ? $"Teslim kaydı yetkili override ile işlendi. Not: {overrideNoteToSave}" : null;
            shipment.ChangeStatus(ShipmentStatus.Delivered, _currentUserService.UserId, statusReason);

            // Stok çıkışı — teslim onayında OnHandQty düşürülür, rezervasyon serbest bırakılır
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

                    var actualQty = line.DeliveredQty > 0 ? line.DeliveredQty : line.OrderedQty;

                    stock.Deduct(actualQty);

                    _context.StockTransactions.Add(new StockTransaction
                    {
                        StockMasterId = stock.Id,
                        Type          = StockTransactionType.ShipmentOut,
                        Qty           = -actualQty,
                        Reference     = $"SHP-{shipment.Id}",
                        Date          = DateTime.UtcNow,
                        Note          = $"Sevkiyat #{shipment.Id} teslim edildi"
                    });
                }
            }

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException(
                    "Stok, aynı anda başka bir işlem tarafından güncellendi. Lütfen tekrar deneyin.");
            }
        }
    }
}
