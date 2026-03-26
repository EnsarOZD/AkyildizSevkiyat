using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Services
{
    /// <summary>
    /// Araç yükleme öncesi son kontrol servisi.
    /// SetZoneDriverInfoCommand'a enjekte edilir ve kalite garantisini sağlar.
    ///
    /// Tasarım kararları:
    /// - "Blok" kontroller exception fırlatır → araç ataması tamamen durur.
    /// - "Uyarı" kontroller exception fırlatmaz → GetSummaryAsync ile döner.
    /// - ReconciliationGuard ile aynı DI/scoped pattern'i izler.
    /// </summary>
    public class PreDispatchGuard
    {
        private readonly IApplicationDbContext _context;

        public PreDispatchGuard(IApplicationDbContext context)
        {
            _context = context;
        }

        // ─── Blok kontroller (exception fırlatır) ────────────────────────────────

        /// <summary>
        /// Zone'daki tüm aktif sevkiyatların ReadyForDispatch statüsünde olmasını zorunlu kılar.
        /// AssignedToWarehouse veya Picking'de kalan sevkiyat varsa araç ataması bloklanır.
        /// </summary>
        public async Task ThrowIfShipmentsNotReadyAsync(int zonePreparationId, CancellationToken ct)
        {
            var notReady = await _context.Shipments
                .Where(s => s.ZonePreparationId == zonePreparationId
                         && s.Status != ShipmentStatus.Cancelled
                         && s.Status != ShipmentStatus.Passive
                         && s.Status != ShipmentStatus.Created
                         && s.Status != ShipmentStatus.ReadyForDispatch
                         && s.Status < ShipmentStatus.Delivered)
                .Select(s => new { s.Id, s.Status })
                .ToListAsync(ct);

            if (notReady.Count > 0)
            {
                var ids = string.Join(", ", notReady.Select(s => $"#{s.Id} ({s.Status})"));
                throw new DomainException(
                    $"Araç ataması yapılamıyor: {notReady.Count} sevkiyat henüz 'Sevke Hazır' " +
                    $"statüsünde değil. ({ids}). " +
                    "Tüm Micro/Macro toplamalar tamamlanmadan araç ataması yapılamaz.");
            }
        }

        /// <summary>
        /// Zone'daki sevkiyatlarda Open + Error seviyesinde ReconciliationIssue varsa bloklar.
        /// Acknowledged sorunlar engel teşkil etmez.
        /// </summary>
        public async Task ThrowIfZoneHasOpenErrorsAsync(int zonePreparationId, CancellationToken ct)
        {
            var shipmentIds = await _context.Shipments
                .Where(s => s.ZonePreparationId == zonePreparationId
                         && s.Status != ShipmentStatus.Cancelled
                         && s.Status != ShipmentStatus.Passive)
                .Select(s => s.Id)
                .ToListAsync(ct);

            if (shipmentIds.Count == 0) return;

            var error = await _context.ReconciliationIssues
                .Where(i => i.Status == ReconciliationStatus.Open
                         && i.Severity == ReconciliationSeverity.Error
                         && i.ShipmentId != null
                         && shipmentIds.Contains(i.ShipmentId.Value))
                .Select(i => new { i.ShipmentId, i.CheckType, i.Description })
                .FirstOrDefaultAsync(ct);

            if (error != null)
                throw new DomainException(
                    $"Araç ataması yapılamıyor: Sevkiyat #{error.ShipmentId} için açık hata mevcut " +
                    $"({error.CheckType}: {error.Description}). " +
                    "Uzlaştırma ekranından sorunu çözün veya Acknowledge edin.");
        }

        // ─── Özet (exception fırlatmaz, PreDispatchSummaryDto döner) ────────────

        /// <summary>
        /// Araç atama modalı açılırken çağrılır.
        /// Operatöre yüklenecek sevkiyat kalitesi hakkında özet sunar.
        /// Blockers doluysa araç ataması UI tarafından bloklanmalıdır.
        /// </summary>
        public async Task<PreDispatchSummaryDto> GetSummaryAsync(int zonePreparationId, CancellationToken ct)
        {
            var shipments = await _context.Shipments
                .Where(s => s.ZonePreparationId == zonePreparationId
                         && s.Status != ShipmentStatus.Cancelled
                         && s.Status != ShipmentStatus.Passive
                         && s.Status != ShipmentStatus.Created)
                .Select(s => new
                {
                    s.Id,
                    s.Status,
                    s.NetsisTransferredAt,
                })
                .ToListAsync(ct);

            var shipmentIds = shipments.Select(s => s.Id).ToList();

            // Satır bazlı istatistikler
            var lineStats = await _context.ShipmentLines
                .Where(l => shipmentIds.Contains(l.ShipmentId)
                         && l.OrderedQty > 0)
                .Select(l => new
                {
                    l.ShipmentId,
                    l.OrderedQty,
                    l.DeliveredQty,
                    l.StockMasterId,
                    HasDiffReason = l.DifferenceReason != null && l.DifferenceReason != string.Empty,
                })
                .ToListAsync(ct);

            // ForceComplete geçilmiş sevkiyatlar: ShipmentHistory'de ForceComplete işareti
            // ForceComplete bilgisi History'de Description alanında saklı; direkt query daha güvenli.
            // Yaklaşım: zero DeliveredQty olan ama ReadyForDispatch'e geçmiş sevkiyatlar → olası ForceComplete
            var zeroPickedShipmentIds = lineStats
                .Where(l => l.DeliveredQty == 0)
                .Select(l => l.ShipmentId)
                .Distinct()
                .ToHashSet();

            var readyShipmentIds = shipments
                .Where(s => s.Status == ShipmentStatus.ReadyForDispatch
                         || s.Status == ShipmentStatus.AssignedToVehicle
                         || s.Status == ShipmentStatus.Delivered)
                .Select(s => s.Id)
                .ToHashSet();

            var forceCompleteShipmentIds = zeroPickedShipmentIds.Intersect(readyShipmentIds).ToList();

            // Reconciliation sorunları
            var reconciliation = await _context.ReconciliationIssues
                .Where(i => i.ShipmentId != null
                         && shipmentIds.Contains(i.ShipmentId.Value)
                         && i.Status == ReconciliationStatus.Open)
                .Select(i => new { i.Severity, i.CheckType, i.Description, i.ShipmentId })
                .ToListAsync(ct);

            var dto = new PreDispatchSummaryDto
            {
                ZonePreparationId = zonePreparationId,
                TotalShipments    = shipments.Count,
                ReadyShipments    = shipments.Count(s =>
                    s.Status == ShipmentStatus.ReadyForDispatch ||
                    s.Status == ShipmentStatus.AssignedToVehicle),
                NotReadyShipments = shipments.Count(s =>
                    s.Status != ShipmentStatus.ReadyForDispatch &&
                    s.Status < ShipmentStatus.AssignedToVehicle),

                NetsisTransferredCount = shipments.Count(s => s.NetsisTransferredAt.HasValue),
                NetsisFailedCount      = shipments.Count(s => !s.NetsisTransferredAt.HasValue),

                ZeroPickedLineCount  = lineStats.Count(l => l.DeliveredQty == 0),
                UnmappedLineCount    = lineStats.Count(l => !l.StockMasterId.HasValue),
                DiffReasonLineCount  = lineStats.Count(l => l.HasDiffReason),
                OverPickedLineCount  = lineStats.Count(l => l.DeliveredQty > l.OrderedQty),

                HasForceCompleteShipments = forceCompleteShipmentIds.Count > 0,
                ForceCompleteShipmentIds  = forceCompleteShipmentIds,

                OpenErrorCount   = reconciliation.Count(i => i.Severity == ReconciliationSeverity.Error),
                OpenWarningCount = reconciliation.Count(i => i.Severity == ReconciliationSeverity.Warning),
            };

            // Blockers (araç atamasını engelleyen durumlar)
            if (dto.NotReadyShipments > 0)
                dto.Blockers.Add($"{dto.NotReadyShipments} sevkiyat henüz 'Sevke Hazır' değil.");

            if (dto.OpenErrorCount > 0)
                dto.Blockers.Add($"{dto.OpenErrorCount} açık hata var (Uzlaştırma ekranında çözülmeli).");

            if (dto.NetsisFailedCount > 0)
                dto.Blockers.Add($"{dto.NetsisFailedCount} sevkiyat Netsis'e aktarılamamış.");

            // Warnings (uyarı ama bloklamaz)
            if (dto.ZeroPickedLineCount > 0)
                dto.Warnings.Add($"{dto.ZeroPickedLineCount} satırda toplama yapılmamış (DeliveredQty=0).");

            if (dto.HasForceCompleteShipments)
                dto.Warnings.Add($"{forceCompleteShipmentIds.Count} sevkiyat eksik toplamayla tamamlandı (ForceComplete).");

            if (dto.UnmappedLineCount > 0)
                dto.Warnings.Add($"{dto.UnmappedLineCount} satırın stok eşlemesi eksik.");

            if (dto.OpenWarningCount > 0)
                dto.Warnings.Add($"{dto.OpenWarningCount} açık uyarı mevcut.");

            return dto;
        }
    }

    public class PreDispatchSummaryDto
    {
        public int ZonePreparationId { get; set; }

        // Sevkiyat sayıları
        public int TotalShipments    { get; set; }
        public int ReadyShipments    { get; set; }
        public int NotReadyShipments { get; set; }

        // Netsis / İrsaliye
        public int NetsisTransferredCount { get; set; }
        public int NetsisFailedCount      { get; set; }

        // Picking kalitesi
        public int  ZeroPickedLineCount          { get; set; }
        public int  UnmappedLineCount            { get; set; }
        public int  DiffReasonLineCount          { get; set; }
        public int  OverPickedLineCount          { get; set; }
        public bool HasForceCompleteShipments    { get; set; }
        public List<int> ForceCompleteShipmentIds { get; set; } = new();

        // Reconciliation
        public int OpenErrorCount   { get; set; }
        public int OpenWarningCount { get; set; }

        // Karar alanları
        public List<string> Blockers { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        public bool CanProceed => Blockers.Count == 0;
    }
}
