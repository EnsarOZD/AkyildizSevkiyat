using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.Warehouse.Services;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.RecordVehicleReturn
{
    public class RecordVehicleReturnCommandHandler : IRequestHandler<RecordVehicleReturnCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ZoneAutoCloseService _zoneAutoClose;

        public RecordVehicleReturnCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            ZoneAutoCloseService zoneAutoClose)
        {
            _context = context;
            _currentUserService = currentUserService;
            _zoneAutoClose = zoneAutoClose;
        }

        public async Task<Unit> Handle(RecordVehicleReturnCommand request, CancellationToken cancellationToken)
        {
            var shipment = await _context.Shipments
                .Include(s => s.Lines)
                .FirstOrDefaultAsync(s => s.Id == request.ShipmentId, cancellationToken);

            if (shipment == null)
                throw new NotFoundException("Shipment", request.ShipmentId);

            if (shipment.Status != ShipmentStatus.AssignedToVehicle 
                && shipment.Status != ShipmentStatus.Dispatched 
                && shipment.Status != ShipmentStatus.Delivered)
                throw new DomainException("Araç iadesi yalnızca 'Araçta', 'Yolda' veya 'Teslim Edildi' durumundaki sevkiyatlara kaydedilebilir.");

            var lineIds = request.Lines.Select(l => l.ShipmentLineId).ToList();
            var lineMap = shipment.Lines.ToDictionary(l => l.Id);

            // Validate all requested line ids belong to this shipment
            var invalidIds = lineIds.Except(lineMap.Keys).ToList();
            if (invalidIds.Count > 0)
                throw new DomainException($"Şu satır ID'leri bu sevkiyata ait değil: {string.Join(", ", invalidIds)}");

            // Driver Ownership & Override
            bool isOverride = false;
            string? overrideNoteToSave = null;

            if (_currentUserService.Role == UserRole.Driver)
            {
                var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == _currentUserService.UserId, cancellationToken)
                    ?? throw new ForbiddenException("Kullanıcıya tanımlı bir şoför kaydı bulunamadı.");

                // Doğrudan atama VEYA sevkiyatın zone'una atanmış şoför (çok şoförlü sefer).
                bool isAssignedDriver = shipment.AssignedDriverId == driver.Id;
                bool isZoneDriver = shipment.ZonePreparationId.HasValue
                    && await _context.ZonePreparationDrivers.AnyAsync(
                        zpd => zpd.ZonePreparationId == shipment.ZonePreparationId.Value
                            && zpd.DriverId == driver.Id,
                        cancellationToken);

                if (!isAssignedDriver && !isZoneDriver)
                {
                    throw new ForbiddenException("Bu sevkiyat size atanmamış.");
                }
            }
            else
            {
                // Driver/Admin processing shipment -> always an override
                isOverride = true;
                if (string.IsNullOrWhiteSpace(request.OverrideNote))
                {
                    // Fallback: If return note is provided, use it as override note for non-drivers
                    if (!string.IsNullOrWhiteSpace(request.ReturnNote))
                    {
                        overrideNoteToSave = request.ReturnNote;
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

            // Collect stock masters for mapped lines
            var stockMasterIds = request.Lines
                .Select(l => lineMap[l.ShipmentLineId])
                .Where(l => l.StockMasterId.HasValue)
                .Select(l => l.StockMasterId!.Value)
                .Distinct()
                .ToList();

            var stocks = stockMasterIds.Count > 0
                ? await _context.StockMasters
                    .Where(s => stockMasterIds.Contains(s.Id))
                    .ToListAsync(cancellationToken)
                : new List<StockMaster>();

            var stockMap = stocks.ToDictionary(s => s.Id);

            // Apply returns to each line
            foreach (var dto in request.Lines)
            {
                var line = lineMap[dto.ShipmentLineId];

                if (dto.ReturnedQty < 0)
                    throw new DomainException($"İade miktarı negatif olamaz (Satır #{dto.ShipmentLineId}).");

                var maxReturnable = (line.DeliveredQty > 0 ? line.DeliveredQty : line.OrderedQty) - (line.ReturnedQty ?? 0);
                if (dto.ReturnedQty > maxReturnable)
                    throw new DomainException($"İade miktarı ({dto.ReturnedQty}) teslim edilen miktarı ({maxReturnable}) aşıyor (Satır #{dto.ShipmentLineId}).");

                line.RecordReturn((line.ReturnedQty ?? 0) + dto.ReturnedQty, dto.ReturnReason);

                // Restore stock only if shipment was previously delivered
                // (ShipmentOut was written at delivery; Dispatched shipments never had ShipmentOut)
                if (shipment.Status == ShipmentStatus.Delivered
                    && line.StockMasterId.HasValue
                    && stockMap.TryGetValue(line.StockMasterId.Value, out var stock))
                {
                    stock.Increase(dto.ReturnedQty);

                    _context.StockTransactions.Add(new StockTransaction
                    {
                        StockMasterId = stock.Id,
                        Type = StockTransactionType.VehicleReturn,
                        Qty = dto.ReturnedQty,
                        Reference = $"SHP-{shipment.Id}",
                        Date = DateTime.UtcNow,
                        Note = $"Sevkiyat #{shipment.Id} araç iadesi — {dto.ReturnReason}"
                    });
                }
            }

            // Set return metadata on shipment
            shipment.RecordVehicleReturn(DateTime.UtcNow, string.IsNullOrWhiteSpace(request.ReturnNote) ? null : request.ReturnNote);

            // Determine if all delivered lines are fully returned → ReturnedToWarehouse
            bool allReturned = shipment.Lines.All(l =>
            {
                var delivered = l.DeliveredQty > 0 ? l.DeliveredQty : l.OrderedQty;
                return (l.ReturnedQty ?? 0) >= delivered;
            });

            if (allReturned)
            {
                string? statusReason = isOverride ? $"Tüm kalemler iade edildi. Yetkili override notu: {overrideNoteToSave}" : "Tüm kalemler iade edildi";
                shipment.ChangeStatus(ShipmentStatus.ReturnedToWarehouse, _currentUserService.UserId, statusReason);
            }
            else
            {
                // Kısmi iade → durum değişmez ama her zaman history kaydı yazılır (driver veya override)
                string description = isOverride
                    ? $"Kısmi araç iadesi yetkili override ile işlendi. Not: {overrideNoteToSave}"
                    : "Kısmi araç iadesi kaydedildi.";

                shipment.Histories.Add(new ShipmentHistory
                {
                    ShipmentId = shipment.Id,
                    OldStatus = shipment.Status,
                    NewStatus = shipment.Status,
                    ChangedByUserId = _currentUserService.UserId,
                    ChangedAt = DateTime.UtcNow,
                    Description = description
                });
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (shipment.ZonePreparationId.HasValue)
            {
                await _zoneAutoClose.TryAutoCloseAsync(shipment.ZonePreparationId.Value, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return Unit.Value;
        }
    }
}
