using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.Warehouse.Services;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.BulkMarkDelivered
{
    public record BulkMarkShipmentsDeliveredCommand : IRequest<BulkMarkDeliveredResult>, IRequireRoles
    {
        public List<int> ShipmentIds { get; init; } = new();
        public string DeliveryRecipient { get; init; } = string.Empty;
        public string OverrideNote { get; init; } = string.Empty;

        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting" };
    }

    public record BulkMarkDeliveredResult(int SuccessCount, List<string> Errors);

    public class BulkMarkShipmentsDeliveredCommandValidator : AbstractValidator<BulkMarkShipmentsDeliveredCommand>
    {
        public BulkMarkShipmentsDeliveredCommandValidator()
        {
            RuleFor(x => x.ShipmentIds).NotEmpty().Must(ids => ids.Count <= 200);
            RuleFor(x => x.DeliveryRecipient).NotEmpty().MaximumLength(200);
            RuleFor(x => x.OverrideNote).NotEmpty().MaximumLength(500);
        }
    }

    public class BulkMarkShipmentsDeliveredCommandHandler
        : IRequestHandler<BulkMarkShipmentsDeliveredCommand, BulkMarkDeliveredResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ZoneAutoCloseService _zoneAutoClose;

        public BulkMarkShipmentsDeliveredCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            ZoneAutoCloseService zoneAutoClose)
        {
            _context = context;
            _currentUserService = currentUserService;
            _zoneAutoClose = zoneAutoClose;
        }

        public async Task<BulkMarkDeliveredResult> Handle(
            BulkMarkShipmentsDeliveredCommand request,
            CancellationToken cancellationToken)
        {
            var errors = new List<string>();

            var shipments = await _context.Shipments
                .Include(s => s.Lines)
                .Where(s => request.ShipmentIds.Contains(s.Id))
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            var overrideReason = $"Toplu teslim işlemi. Not: {request.OverrideNote}";

            // Collect all stock deductions across all shipments
            var stockDeductions = new Dictionary<int, decimal>(); // stockMasterId → qty
            var stockTransactions = new List<(int stockMasterId, decimal qty, int shipmentId)>();

            int successCount = 0;

            foreach (var shipment in shipments)
            {
                try
                {
                    if (shipment.Status == ShipmentStatus.Delivered)
                        continue; // idempotent

                    bool allowedStatus =
                        shipment.Status == ShipmentStatus.Dispatched ||
                        shipment.Status == ShipmentStatus.AssignedToVehicle ||
                        shipment.Status == ShipmentStatus.ReadyForDispatch;

                    if (!allowedStatus)
                    {
                        errors.Add($"#{shipment.Id}: Geçersiz durum ({shipment.Status}). Yalnızca 'Yolda', 'Araçta' veya 'Sevke Hazır' durumundaki sevkiyatlar işlenebilir.");
                        continue;
                    }

                    shipment.RecordDelivery(
                        now,
                        request.DeliveryRecipient,
                        null,
                        null,
                        _currentUserService.UserId,
                        _currentUserService.Role?.ToString(),
                        request.OverrideNote,
                        null,
                        null,
                        null);

                    shipment.ChangeStatus(ShipmentStatus.Delivered, _currentUserService.UserId, overrideReason);

                    foreach (var line in shipment.Lines.Where(l => l.StockMasterId.HasValue))
                    {
                        var qty = line.DeliveredQty > 0 ? line.DeliveredQty : line.OrderedQty;
                        var smId = line.StockMasterId!.Value;
                        stockDeductions[smId] = stockDeductions.GetValueOrDefault(smId) + qty;
                        stockTransactions.Add((smId, qty, shipment.Id));
                    }

                    successCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"#{shipment.Id}: {ex.Message}");
                }
            }

            var foundIds = shipments.Select(s => s.Id).ToHashSet();
            foreach (var id in request.ShipmentIds.Where(id => !foundIds.Contains(id)))
                errors.Add($"#{id}: Sevkiyat bulunamadı.");

            // Apply stock deductions in batch
            if (stockDeductions.Count > 0)
            {
                var stocks = await _context.StockMasters
                    .Where(s => stockDeductions.Keys.Contains(s.Id))
                    .ToListAsync(cancellationToken);

                var stockMap = stocks.ToDictionary(s => s.Id);

                foreach (var (smId, totalQty) in stockDeductions)
                {
                    if (!stockMap.TryGetValue(smId, out var stock)) continue;
                    stock.Deduct(totalQty);
                }

                foreach (var (smId, qty, shipmentId) in stockTransactions)
                {
                    _context.StockTransactions.Add(new StockTransaction
                    {
                        StockMasterId = smId,
                        Type          = StockTransactionType.ShipmentOut,
                        Qty           = -qty,
                        Reference     = $"SHP-{shipmentId}",
                        Date          = now,
                        Note          = $"Toplu teslim — Sevkiyat #{shipmentId}"
                    });
                }
            }

            if (successCount > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);

                // Etkilenen zone'ları toplu kapat
                var zoneIds = shipments
                    .Where(s => s.ZonePreparationId.HasValue)
                    .Select(s => s.ZonePreparationId!.Value)
                    .Distinct();
                foreach (var zoneId in zoneIds)
                    await _zoneAutoClose.TryAutoCloseAsync(zoneId, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return new BulkMarkDeliveredResult(successCount, errors);
        }
    }
}
