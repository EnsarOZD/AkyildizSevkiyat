using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.Reconciliation.Services;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.MarkZoneMacroReady
{
    public record MarkZoneMacroReadyCommand : IRequest<MarkZoneMacroReadyResult>, IRequireRoles
    {
        public int ZonePreparationId { get; init; }
        /// <summary>
        /// Eksik macro satırlar olsa bile devam et (kasıtlı eksik toplama).
        /// ForceReason zorunludur.
        /// </summary>
        public bool ForceComplete { get; init; } = false;
        public string? ForceReason { get; init; }

        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Warehouse" };
    }

    public class MarkZoneMacroReadyResult
    {
        public bool Success { get; set; }
        /// <summary>DeliveredQty = 0 olan macro satır sayısı.</summary>
        public int UnfilledMacroLineCount { get; set; }
    }

    public class MarkZoneMacroReadyCommandHandler : IRequestHandler<MarkZoneMacroReadyCommand, MarkZoneMacroReadyResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ReconciliationGuard _guard;

        public MarkZoneMacroReadyCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            ReconciliationGuard guard)
        {
            _context = context;
            _currentUserService = currentUserService;
            _guard = guard;
        }

        public async Task<MarkZoneMacroReadyResult> Handle(MarkZoneMacroReadyCommand request, CancellationToken cancellationToken)
        {
            // ── Enforcement: Zone'daki herhangi bir sevkiyatta ISS uyumsuzluğu varsa blokla ──
            await _guard.ThrowIfIssQtyMismatchForZoneAsync(request.ZonePreparationId, cancellationToken);

            var zp = await _context.ZonePreparations
                .Include(z => z.Projects)
                .FirstOrDefaultAsync(z => z.Id == request.ZonePreparationId, cancellationToken);

            if (zp == null) throw new NotFoundException("ZonePreparation", request.ZonePreparationId);

            if (zp.Status < ZonePreparationStatus.MicroReady)
                throw new DomainException("Macro toplama aşamasına geçebilmek için Micro toplamanın tamamlanmış olması gerekir.");

            if (zp.Status >= ZonePreparationStatus.ReadyForDriverInfo)
                throw new DomainException("Bu hazırlık zaten tamamlanmış veya şoför atama aşamasında.");

            if (zp.Projects.Any(p => !p.IsMicroReady))
                throw new DomainException("Hâlâ tamamlanmamış Micro toplama yapılan projeler var.");

            // Tamamlanma kontrolü — bu zone'un Macro satırlarını sorgula
            var macroLinesData = await _context.ShipmentLines
                .Where(l => l.Shipment.ZonePreparationId == zp.Id
                         && l.Shipment.Status != ShipmentStatus.Cancelled
                         && l.Shipment.Status != ShipmentStatus.Passive
                         && l.OrderedQty > 0)
                .Select(l => new { 
                    l.Id, 
                    l.DeliveredQty, 
                    l.StockMasterId, 
                    StockCode = l.IssOrderLine != null ? l.IssOrderLine.StockCode : l.StockCode,
                    PickingType = l.StockMaster != null ? (PickingType?)l.StockMaster.PickingType : null
                })
                .ToListAsync(cancellationToken);

            var externalCodes = macroLinesData.Select(l => l.StockCode).Distinct().ToList();
            var mappings = await _context.StockMappings
                .Include(m => m.LocalStock)
                .Where(m => externalCodes.Contains(m.ExternalStockCode))
                .ToListAsync(cancellationToken);

            var macroLines = macroLinesData.Where(line => {
                if (line.StockMasterId.HasValue && line.PickingType.HasValue)
                {
                    return line.PickingType == PickingType.Macro;
                }
                var mapping = mappings.FirstOrDefault(m => m.ExternalStockCode.Equals(line.StockCode, StringComparison.OrdinalIgnoreCase));
                if (mapping != null && mapping.LocalStock != null)
                {
                    return mapping.LocalStock.PickingType == PickingType.Macro;
                }
                return false; // Default is Micro
            }).ToList();

            var unfilledMacroCount = macroLines.Count(l => l.DeliveredQty == 0);

            if (unfilledMacroCount > 0 && !request.ForceComplete)
                throw new DomainException(
                    $"{unfilledMacroCount} makro satır henüz toplanmadı. " +
                    "Eksik satırlarla devam etmek için ForceComplete=true ve ForceReason gönderin.");

            if (request.ForceComplete && string.IsNullOrWhiteSpace(request.ForceReason))
                throw new DomainException("ForceComplete=true kullanılırken ForceReason zorunludur.");

            zp.Status = ZonePreparationStatus.ReadyForDriverInfo;
            
            var shipments = await _context.Shipments
                .Include(s => s.Project) // Need Project to check ZoneId if querying by Zone
                .Where(s => 
                    s.ZonePreparationId == zp.Id && // Use exact Batch ID
                    s.Status < ShipmentStatus.ReadyForDispatch &&
                    s.Status != ShipmentStatus.Cancelled &&
                    s.Status != ShipmentStatus.Passive
                )
                .ToListAsync(cancellationToken);

            foreach(var s in shipments)
            {
                s.ChangeStatus(ShipmentStatus.ReadyForDispatch, _currentUserService.UserId);
            }
            
            await _context.SaveChangesAsync(cancellationToken);

            return new MarkZoneMacroReadyResult
            {
                Success               = true,
                UnfilledMacroLineCount = unfilledMacroCount,
            };
        }
    }
}
