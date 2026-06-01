using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.Warehouse.Services;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using System.Collections.Generic;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.MarkShipmentDelivered
{
    public sealed record MarkShipmentDeliveredCommand(
        int ShipmentId,
        string? DeliveryNote = null,
        string? DeliveryRecipient = null,
        List<string>? DeliveryPhotosBase64 = null,
        string? OverrideNote = null,
        double? DeliveryLatitude = null,
        double? DeliveryLongitude = null
    ) : IRequest;

    public sealed class MarkShipmentDeliveredCommandHandler
        : IRequestHandler<MarkShipmentDeliveredCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPhotoStorageService _photos;
        private readonly ZoneAutoCloseService _zoneAutoClose;

        public MarkShipmentDeliveredCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IPhotoStorageService photos,
            ZoneAutoCloseService zoneAutoClose)
        {
            _context = context;
            _currentUserService = currentUserService;
            _photos = photos;
            _zoneAutoClose = zoneAutoClose;
        }

        public async Task Handle(MarkShipmentDeliveredCommand request, CancellationToken cancellationToken)
        {
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
            bool isClothingReadyForDispatch = shipment.OperationType == Domain.Enums.OperationType.Clothing
                                           && shipment.Status == ShipmentStatus.ReadyForDispatch;
            if (shipment.Status != ShipmentStatus.Dispatched
                && shipment.Status != ShipmentStatus.AssignedToVehicle
                && !isClothingReadyForDispatch)
            {
                throw new DomainException("Yalnızca 'Yolda', 'Araçta' veya kıyafet operasyonunda 'Sevke Hazır' durumundaki sevkiyatlar teslim edilebilir.");
            }

            // 5. FIX — Delivery Proof (MINIMUM)
            if (string.IsNullOrWhiteSpace(request.DeliveryRecipient))
            {
                throw new DomainException("Teslim alan bilgisi zorunludur.");
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
                    throw new ForbiddenException("Bu sevkiyat size atanmamış.");
                }

                var hasAnyPhoto = request.DeliveryPhotosBase64?.Any(p => !string.IsNullOrWhiteSpace(p)) ?? false;
                if (!hasAnyPhoto)
                {
                    throw new DomainException("En az bir teslim fotoğrafı zorunludur.");
                }
            }
            else
            {
                // Driver/Admin processing shipment -> always an override
                isOverride = true;
                if (string.IsNullOrWhiteSpace(request.OverrideNote))
                {
                    // Fallback to delivery note if override note is missing
                    if (!string.IsNullOrWhiteSpace(request.DeliveryNote))
                    {
                        overrideNoteToSave = request.DeliveryNote;
                    }
                    else
                    {
                        throw new DomainException("Yönetici/Operasyon işlemi için bir açıklama (Not) girmek zorunludur.");
                    }
                }
                else
                {
                    overrideNoteToSave = request.OverrideNote;
                }
            }
        

            shipment.RecordDelivery(
                DateTime.UtcNow,
                request.DeliveryRecipient!,
                request.DeliveryNote,
                null,
                _currentUserService.UserId,
                _currentUserService.Role?.ToString(),
                overrideNoteToSave,
                null,
                request.DeliveryLatitude,
                request.DeliveryLongitude);

            // Save delivery photos (up to 5) to dedicated table.
            // Track saved paths so we can clean up if SaveChangesAsync rolls back.
            var photos = request.DeliveryPhotosBase64?.Where(p => !string.IsNullOrWhiteSpace(p)).Take(5).ToList()
                         ?? new List<string>();
            var savedPhotoPaths = new List<string>(photos.Count);
            for (int i = 0; i < photos.Count; i++)
            {
                var path = await _photos.SaveDeliveryPhotoAsync(
                    photos[i], shipment.Id, shipment.IrsaliyeNo, i + 1, cancellationToken);
                savedPhotoPaths.Add(path);
                _context.ShipmentDeliveryPhotos.Add(new ShipmentDeliveryPhoto
                {
                    ShipmentId = shipment.Id,
                    PhotoPath  = path,
                    PhotoIndex = i + 1,
                    TakenAt    = DateTime.UtcNow,
                });
            }

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
                foreach (var path in savedPhotoPaths)
                    await _photos.DeleteAsync(path);
                throw new ConflictException(
                    "Stok, aynı anda başka bir işlem tarafından güncellendi. Lütfen tekrar deneyin.");
            }
            catch
            {
                foreach (var path in savedPhotoPaths)
                    await _photos.DeleteAsync(path);
                throw;
            }

            // Tüm sevkiyatlar final duruma geçtiyse zone'u otomatik kapat
            if (shipment.ZonePreparationId.HasValue)
            {
                await _zoneAutoClose.TryAutoCloseAsync(shipment.ZonePreparationId.Value, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
