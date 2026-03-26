using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Reconciliation.Services
{
    /// <summary>
    /// Uzlaştırma zorlama servisi.
    /// Kritik operasyonlar öncesinde gerçek zamanlı tutarlılık kontrolleri yapar.
    ///
    /// Tasarım kararları:
    /// - PickingIncomplete + IssQtyMismatch: gerçek zamanlı DB sorgusu (taze veri)
    /// - NetsisExport: ReconciliationIssues tablosu sorgusu (admin Acknowledge sonrası engellenemez)
    ///
    /// Kullanım: DI'a AddScoped olarak kayıt edilir.
    /// </summary>
    public class ReconciliationGuard
    {
        private readonly IApplicationDbContext _context;

        public ReconciliationGuard(IApplicationDbContext context)
        {
            _context = context;
        }

        // ─── Enforcement: Throws ─────────────────────────────────────────────────

        /// <summary>
        /// MarkShipmentDelivered öncesinde çağrılır.
        /// DeliveredQty = 0 olan satır varsa teslim engellenir.
        /// </summary>
        public async Task ThrowIfPickingIncompleteAsync(int shipmentId, CancellationToken ct)
        {
            var issue = await CheckPickingIncompleteAsync(shipmentId, ct);
            if (issue != null)
                throw new DomainException(
                    $"Sevkiyat #{shipmentId} teslim edilemiyor: {issue.Reason} " +
                    "Tüm satırların toplama miktarı girilmeli ya da iade kaydı oluşturulmalı.");
        }

        /// <summary>
        /// MarkReady (tek sevkiyat) ve MarkZoneMacroReady öncesinde çağrılır.
        /// ShipmentLine.OrderedQty ≠ IssOrderLine.OrderedQty ise bloklar.
        /// </summary>
        public async Task ThrowIfIssQtyMismatchAsync(int shipmentId, CancellationToken ct)
        {
            var issue = await CheckIssQtyMismatchAsync(shipmentId, ct);
            if (issue != null)
                throw new DomainException(
                    $"Sevkiyat #{shipmentId} hazır işaretlenemiyor: {issue.Reason} " +
                    "Uzlaştırma ekranından bu sorunu çözün veya Acknowledge edin.");
        }

        /// <summary>
        /// MarkZoneMacroReady öncesinde zone'daki TÜM sevkiyatlar kontrol edilir.
        /// </summary>
        public async Task ThrowIfIssQtyMismatchForZoneAsync(int zonePreparationId, CancellationToken ct)
        {
            var mismatch = await _context.ShipmentLines
                .Where(sl => sl.Shipment.ZonePreparationId == zonePreparationId
                          && sl.Shipment.Status != ShipmentStatus.Cancelled
                          && sl.Shipment.Status != ShipmentStatus.Passive
                          && sl.IssOrderLineId != null
                          && sl.OrderedQty != sl.IssOrderLine!.OrderedQty)
                .Select(sl => new
                {
                    sl.ShipmentId,
                    sl.StockName,
                    ShipmentQty = sl.OrderedQty,
                    IssQty      = sl.IssOrderLine!.OrderedQty,
                })
                .FirstOrDefaultAsync(ct);

            if (mismatch != null)
                throw new DomainException(
                    $"Zone hazır işaretlenemiyor: Sevkiyat #{mismatch.ShipmentId}, '{mismatch.StockName}' satırında " +
                    $"ISS miktar uyumsuzluğu (Sevkiyat: {mismatch.ShipmentQty:0.##}, ISS: {mismatch.IssQty:0.##}). " +
                    "Uzlaştırma ekranından bu sorunu çözün veya Acknowledge edin.");
        }

        /// <summary>
        /// ExportShipmentToNetsis öncesinde çağrılır.
        /// Open + Error seviyesinde kayıtlı sorun varsa aktarımı engeller.
        /// Acknowledge edilmiş sorunlar engel teşkil etmez.
        /// </summary>
        public async Task ThrowIfOpenErrorIssuesAsync(int shipmentId, CancellationToken ct)
        {
            var lineIds = await _context.ShipmentLines
                .Where(sl => sl.ShipmentId == shipmentId)
                .Select(sl => sl.Id)
                .ToListAsync(ct);

            var blocker = await _context.ReconciliationIssues
                .Where(i => i.Status == ReconciliationStatus.Open
                         && i.Severity == ReconciliationSeverity.Error
                         && (i.ShipmentId == shipmentId
                             || (i.ShipmentLineId != null && lineIds.Contains(i.ShipmentLineId.Value))))
                .Select(i => new { i.CheckType, i.Description })
                .FirstOrDefaultAsync(ct);

            if (blocker != null)
                throw new DomainException(
                    $"Sevkiyat #{shipmentId} Netsis'e aktarılamıyor: " +
                    $"Açık hata mevcut ({blocker.CheckType}: {blocker.Description}). " +
                    "Uzlaştırma ekranından sorunu çözün veya Acknowledge edin.");
        }

        // ─── CanShipmentProceed (blokları toplar, exception atmaz) ───────────────

        public async Task<CanShipmentProceedResult> GetBlockingIssuesAsync(int shipmentId, CancellationToken ct)
        {
            var result = new CanShipmentProceedResult();

            var picking  = await CheckPickingIncompleteAsync(shipmentId, ct);
            var issQty   = await CheckIssQtyMismatchAsync(shipmentId, ct);
            var netsisErr = await CheckOpenErrorIssuesAsync(shipmentId, ct);

            if (picking != null)   result.Blocks.Add(picking);
            if (issQty != null)    result.Blocks.Add(issQty);
            if (netsisErr != null) result.Blocks.Add(netsisErr);

            result.CanMarkDelivered        = picking == null;
            result.CanMarkReadyForDispatch = issQty == null;
            result.CanExportToNetsis       = netsisErr == null;

            return result;
        }

        // ─── Private helpers (return null = no issue) ────────────────────────────

        private async Task<BlockInfo?> CheckPickingIncompleteAsync(int shipmentId, CancellationToken ct)
        {
            var line = await _context.ShipmentLines
                .Where(sl => sl.ShipmentId == shipmentId
                          && sl.OrderedQty > 0
                          && sl.DeliveredQty == 0
                          && (sl.ReturnedQty == null || sl.ReturnedQty == 0))
                .Select(sl => new { sl.StockName, sl.OrderedQty })
                .FirstOrDefaultAsync(ct);

            if (line == null) return null;

            return new BlockInfo
            {
                Gate   = "Delivered",
                Reason = $"'{line.StockName}' satırı toplanmamış (Sipariş: {line.OrderedQty:0.##}).",
            };
        }

        private async Task<BlockInfo?> CheckIssQtyMismatchAsync(int shipmentId, CancellationToken ct)
        {
            var mismatch = await _context.ShipmentLines
                .Where(sl => sl.ShipmentId == shipmentId
                          && sl.IssOrderLineId != null
                          && sl.OrderedQty != sl.IssOrderLine!.OrderedQty)
                .Select(sl => new
                {
                    sl.StockName,
                    ShipmentQty = sl.OrderedQty,
                    IssQty      = sl.IssOrderLine!.OrderedQty,
                })
                .FirstOrDefaultAsync(ct);

            if (mismatch == null) return null;

            return new BlockInfo
            {
                Gate   = "ReadyForDispatch",
                Reason = $"'{mismatch.StockName}' ISS miktar uyumsuzluğu " +
                         $"(Sevkiyat: {mismatch.ShipmentQty:0.##}, ISS: {mismatch.IssQty:0.##}).",
            };
        }

        private async Task<BlockInfo?> CheckOpenErrorIssuesAsync(int shipmentId, CancellationToken ct)
        {
            var lineIds = await _context.ShipmentLines
                .Where(sl => sl.ShipmentId == shipmentId)
                .Select(sl => sl.Id)
                .ToListAsync(ct);

            var blocker = await _context.ReconciliationIssues
                .Where(i => i.Status == ReconciliationStatus.Open
                         && i.Severity == ReconciliationSeverity.Error
                         && (i.ShipmentId == shipmentId
                             || (i.ShipmentLineId != null && lineIds.Contains(i.ShipmentLineId.Value))))
                .Select(i => new { i.CheckType, i.Description })
                .FirstOrDefaultAsync(ct);

            if (blocker == null) return null;

            return new BlockInfo
            {
                Gate   = "NetsisExport",
                Reason = $"Açık hata: {blocker.CheckType} — {blocker.Description}",
            };
        }
    }

    public class CanShipmentProceedResult
    {
        public bool CanMarkDelivered        { get; set; } = true;
        public bool CanMarkReadyForDispatch { get; set; } = true;
        public bool CanExportToNetsis       { get; set; } = true;
        public List<BlockInfo> Blocks       { get; set; } = new();
    }

    public class BlockInfo
    {
        /// <summary>"Delivered" | "ReadyForDispatch" | "NetsisExport"</summary>
        public string Gate   { get; set; } = null!;
        public string Reason { get; set; } = null!;
    }
}
