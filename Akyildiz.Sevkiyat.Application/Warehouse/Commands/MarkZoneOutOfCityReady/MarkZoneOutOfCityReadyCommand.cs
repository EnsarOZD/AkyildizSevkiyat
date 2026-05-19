using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.MarkZoneOutOfCityReady
{
    public record OutOfCityLineUpdateDto(int ShipmentLineId, decimal DeliveredQty, string? DifferenceReason, int? NewLocalStockId = null);

    public record MarkZoneOutOfCityReadyCommand : IRequest<MarkZoneOutOfCityReadyResult>, IRequireRoles
    {
        public int ZonePreparationId { get; init; }
        public List<OutOfCityLineUpdateDto> Lines { get; init; } = new();
        public bool ForceComplete { get; init; } = false;
        public string? ForceReason { get; init; }

        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting", "Warehouse" };
    }

    public class MarkZoneOutOfCityReadyResult
    {
        public bool Success { get; set; }
        public int UnfilledLineCount { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    public class MarkZoneOutOfCityReadyCommandValidator : AbstractValidator<MarkZoneOutOfCityReadyCommand>
    {
        public MarkZoneOutOfCityReadyCommandValidator()
        {
            RuleFor(x => x.ZonePreparationId).GreaterThan(0);
        }
    }

    public class MarkZoneOutOfCityReadyCommandHandler : IRequestHandler<MarkZoneOutOfCityReadyCommand, MarkZoneOutOfCityReadyResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public MarkZoneOutOfCityReadyCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<MarkZoneOutOfCityReadyResult> Handle(MarkZoneOutOfCityReadyCommand request, CancellationToken cancellationToken)
        {
            var warnings = new List<string>();

            var zp = await _context.ZonePreparations
                .Include(z => z.Projects)
                .Include(z => z.Zone)
                .FirstOrDefaultAsync(z => z.Id == request.ZonePreparationId, cancellationToken)
                ?? throw new NotFoundException("ZonePreparation", request.ZonePreparationId);

            if (!zp.IsFrozen)
                throw new DomainException("Hazırlık başlatılmadan tamamlanamaz.");

            if (zp.Status >= ZonePreparationStatus.ReadyForDriverInfo)
                throw new DomainException("Bu hazırlık zaten tamamlanmış veya şoför atama aşamasında.");

            if (zp.Zone?.IsOutOfCity != true)
                throw new DomainException("Bu komut yalnızca şehir dışı bölgeler için kullanılabilir.");

            if (request.ForceComplete && string.IsNullOrWhiteSpace(request.ForceReason))
                throw new DomainException("ForceComplete=true kullanılırken ForceReason zorunludur.");

            // Update shipment line quantities
            var lineUpdateMap = request.Lines.ToDictionary(l => l.ShipmentLineId);

            if (lineUpdateMap.Count > 0)
            {
                var lineIds = lineUpdateMap.Keys.ToList();
                var dbLines = await _context.ShipmentLines
                    .Where(l => lineIds.Contains(l.Id))
                    .ToListAsync(cancellationToken);

                // Pre-load substitute stocks in one query
                var substituteStockIds = lineUpdateMap.Values
                    .Where(u => u.NewLocalStockId.HasValue)
                    .Select(u => u.NewLocalStockId!.Value)
                    .Distinct()
                    .ToList();

                var substituteStocks = substituteStockIds.Count > 0
                    ? await _context.StockMasters
                        .Where(s => substituteStockIds.Contains(s.Id))
                        .ToListAsync(cancellationToken)
                    : new List<Domain.Entities.StockMaster>();

                foreach (var line in dbLines)
                {
                    if (!lineUpdateMap.TryGetValue(line.Id, out var update)) continue;

                    if (update.NewLocalStockId.HasValue)
                    {
                        var stock = substituteStocks.FirstOrDefault(s => s.Id == update.NewLocalStockId.Value);
                        if (stock != null)
                            line.UpdateStockInfo(stock.StockCode, stock.StockName, stock.Unit, stock.Id, true);
                    }

                    line.SetDeliveredQty(update.DeliveredQty, update.DifferenceReason);
                }
            }

            // Count unfilled lines + collect over-delivery warnings
            var allLineData = await _context.ShipmentLines
                .Where(l =>
                    l.Shipment.ZonePreparationId == zp.Id &&
                    l.Shipment.Status != ShipmentStatus.Cancelled &&
                    l.Shipment.Status != ShipmentStatus.Passive &&
                    l.OrderedQty > 0)
                .Select(l => new { l.Id, l.StockName, l.OrderedQty, l.DeliveredQty })
                .ToListAsync(cancellationToken);

            var unfilledCount = allLineData.Count(l => l.DeliveredQty == 0);

            foreach (var l in allLineData.Where(l => l.DeliveredQty > l.OrderedQty))
                warnings.Add($"'{l.StockName}': sipariş {l.OrderedQty:0.##}, verilen {l.DeliveredQty:0.##} (fazla verildi).");

            if (unfilledCount > 0 && !request.ForceComplete)
                throw new DomainException(
                    $"{unfilledCount} satır henüz toplanmadı. " +
                    "Eksik satırlarla devam etmek için ForceComplete=true ve ForceReason gönderin.");

            // Mark all projects as micro-ready
            var preparedBy = _currentUserService.FullName;
            foreach (var project in zp.Projects)
            {
                project.IsMicroReady = true;
                project.MicroReadyAt = DateTime.UtcNow;
                project.PreparedByUserName = preparedBy;
                project.ReleaseLock();
            }

            // Advance zone to ReadyForDriverInfo (skip micro/macro phases)
            zp.Status = ZonePreparationStatus.ReadyForDriverInfo;

            // Mark all shipments as ReadyForDispatch
            var shipments = await _context.WarehouseShipments
                .Where(s =>
                    s.ZonePreparationId == zp.Id &&
                    s.Status < ShipmentStatus.ReadyForDispatch &&
                    s.Status != ShipmentStatus.Cancelled &&
                    s.Status != ShipmentStatus.Passive)
                .ToListAsync(cancellationToken);

            foreach (var s in shipments)
                s.ChangeStatus(ShipmentStatus.ReadyForDispatch, _currentUserService.UserId);

            await _context.SaveChangesAsync(cancellationToken);

            return new MarkZoneOutOfCityReadyResult
            {
                Success = true,
                UnfilledLineCount = unfilledCount,
                Warnings = warnings
            };
        }
    }
}
