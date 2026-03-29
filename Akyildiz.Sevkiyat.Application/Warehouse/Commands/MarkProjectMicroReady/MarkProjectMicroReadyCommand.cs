using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.MarkProjectMicroReady
{
    public record MarkProjectMicroReadyCommand(
        int ZonePreparationProjectId,
        /// <summary>
        /// Eksik satırlar olsa bile devam et.
        /// Kasıtlı eksik toplama durumlarında (ürün yok, hasar, vb.) ForceReason ile birlikte kullanılır.
        /// </summary>
        bool ForceComplete = false,
        string? ForceReason = null
    ) : IRequest<MarkProjectMicroReadyResult>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Warehouse", "Dispatcher" };
    }

    public class MarkProjectMicroReadyResult
    {
        public bool Success { get; set; }
        /// <summary>DeliveredQty = 0 olan satır sayısı (0 ise tüm satırlar toplandı).</summary>
        public int UnfilledLineCount { get; set; }
        /// <summary>StockMapping eksik olan satır sayısı (bu satırlar pick listesine girmedi).</summary>
        public int UnmappedLineCount { get; set; }
    }

    public class MarkProjectMicroReadyCommandHandler : IRequestHandler<MarkProjectMicroReadyCommand, MarkProjectMicroReadyResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public MarkProjectMicroReadyCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<MarkProjectMicroReadyResult> Handle(MarkProjectMicroReadyCommand request, CancellationToken cancellationToken)
        {
            var projectPrep = await _context.ZonePreparationProjects
                .Include(p => p.ZonePreparation)
                .ThenInclude(zp => zp.Projects)
                .FirstOrDefaultAsync(p => p.Id == request.ZonePreparationProjectId, cancellationToken);

            if (projectPrep == null) throw new NotFoundException("ZonePreparationProject", request.ZonePreparationProjectId);

            if (!projectPrep.ZonePreparation.IsFrozen)
                throw new DomainException("Hazırlık başlatılmadan (freeze edilmeden) Micro hazır olarak işaretlenemez.");

            // Tamamlanma kontrolü — bu projenin satırlarını sorgula
            var projectLinesData = await _context.ShipmentLines
                .Where(l => l.Shipment.ProjectId == projectPrep.ProjectId
                         && l.Shipment.ZonePreparationId == projectPrep.ZonePreparationId
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

            var externalCodes = projectLinesData.Select(l => l.StockCode).Distinct().ToList();
            var mappings = await _context.StockMappings
                .Include(m => m.LocalStock)
                .Where(m => externalCodes.Contains(m.ExternalStockCode))
                .ToListAsync(cancellationToken);

            var microLines = projectLinesData.Where(line => {
                if (line.StockMasterId.HasValue && line.PickingType.HasValue)
                {
                    return line.PickingType != PickingType.Macro;
                }
                var mapping = mappings.FirstOrDefault(m => m.ExternalStockCode.Equals(line.StockCode, StringComparison.OrdinalIgnoreCase));
                if (mapping != null && mapping.LocalStock != null)
                {
                    return mapping.LocalStock.PickingType != PickingType.Macro;
                }
                return true; // Default to Micro
            }).ToList();

            var unfilledCount = microLines.Count(l => l.DeliveredQty == 0);

            // Mapping eksik satırlar: StockMasterId null olan veya StockMapping bulunamayanlar
            var unmappedCount = microLines.Count(l => !l.StockMasterId.HasValue && 
                !mappings.Any(m => m.ExternalStockCode.Equals(l.StockCode, StringComparison.OrdinalIgnoreCase) && 
                                  (m.MatchStatus == MatchStatus.Mapped || m.MatchStatus == MatchStatus.Ignored)));

            if (unfilledCount > 0 && !request.ForceComplete)
                throw new DomainException(
                    $"{unfilledCount} satır henüz toplanmadı. " +
                    "Eksik satırlarla devam etmek için ForceComplete=true ve ForceReason gönderin.");

            if (request.ForceComplete && string.IsNullOrWhiteSpace(request.ForceReason))
                throw new DomainException("ForceComplete=true kullanılırken ForceReason zorunludur.");

            // 1. Mark as Ready
            projectPrep.IsMicroReady = true;
            projectPrep.MicroReadyAt = DateTime.UtcNow;

            // 2. Check Parent Status Transition
            // If parent status is lower than MicroReady (e.g. Draft or MicroPicking), we might update it.
            // Actually, if ALL projects are ready, we assume we can move to 'MacroPicking' or 'MicroReady' (intermediate step?)
            // Roadmap says: "Tüm projeler micro-ready olmadan macro aşamasına geçilemez"
            // So if all ready -> Status = MacroPicking (or user triggers it? Let's auto trigger for flow smoothness or just set flag)
            
            // Let's set parent status to 'MicroReady' (2) if all are ready.
            // Then user can click "Start Macro Picking" which sets it to 'MacroPicking' (3)?
            // Or auto-advance to MacroPicking? 
            // Let's check the Enum: MicroPicking(1) -> MicroReady(2) -> MacroPicking(3).
            // Auto-advance to MicroReady(2) seems correct. The Dashboard will likely show "Start Macro" button then.

            if (projectPrep.ZonePreparation.Status == ZonePreparationStatus.Draft)
            {
                projectPrep.ZonePreparation.Status = ZonePreparationStatus.MicroPicking;
            }

            var allReady = projectPrep.ZonePreparation.Projects.All(p => p.IsMicroReady);
            
            if (allReady)
            {
                // Auto-advance to MacroPicking as requested
                projectPrep.ZonePreparation.Status = ZonePreparationStatus.MacroPicking;
            }

            // Determine Target Status based on Zone Status
            var targetStatus = ShipmentStatus.Picking;
            if (projectPrep.ZonePreparation.Status >= ZonePreparationStatus.ReadyForTransfer)
            {
                targetStatus = ShipmentStatus.AssignedToVehicle;
            }
            else if (projectPrep.ZonePreparation.Status >= ZonePreparationStatus.ReadyForDriverInfo)
            {
                targetStatus = ShipmentStatus.ReadyForDispatch;
            }

            // Sync Shipment Status for this Project
            var shipments = await _context.Shipments
                .Where(s => s.ProjectId == projectPrep.ProjectId && 
                            s.ZonePreparationId == projectPrep.ZonePreparationId && // Added Filter
                            s.Status < targetStatus && 
                            s.Status != ShipmentStatus.Created && 
                            s.Status != ShipmentStatus.Passive)
                .ToListAsync(cancellationToken);
            
            foreach(var s in shipments)
            {
                // Use ChangeStatus method as property setter is private
                s.ChangeStatus(targetStatus, _currentUserService.UserId); // System update
            }

            await _context.SaveChangesAsync(cancellationToken);

            return new MarkProjectMicroReadyResult
            {
                Success          = true,
                UnfilledLineCount = unfilledCount,
                UnmappedLineCount = unmappedCount,
            };
        }
    }
}
