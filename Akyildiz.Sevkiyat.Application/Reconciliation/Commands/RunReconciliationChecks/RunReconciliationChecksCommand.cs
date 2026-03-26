using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Reconciliation.Commands.RunReconciliationChecks
{
    /// <summary>
    /// Tüm operasyonel tutarsızlık kontrollerini çalıştırır.
    /// Mevcut Open sorunlarla karşılaştırarak upsert yapar:
    ///   - Yeni bulunan sorunlar INSERT edilir.
    ///   - Artık geçerli olmayan Open sorunlar AutoResolved'a çekilir.
    ///   - Acknowledged sorunlara dokunulmaz.
    /// </summary>
    public record RunReconciliationChecksCommand(
        DateTime? FromDate = null,
        DateTime? ToDate = null
    ) : IRequest<RunReconciliationChecksResult>;

    public class RunReconciliationChecksResult
    {
        public int NewIssuesFound { get; set; }
        public int AutoResolved { get; set; }
        public int TotalOpenAfter { get; set; }
        public Dictionary<string, int> ByCheckType { get; set; } = new();
    }

    public class RunReconciliationChecksCommandHandler
        : IRequestHandler<RunReconciliationChecksCommand, RunReconciliationChecksResult>
    {
        private readonly IApplicationDbContext _context;

        public RunReconciliationChecksCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RunReconciliationChecksResult> Handle(
            RunReconciliationChecksCommand request, CancellationToken ct)
        {
            var fromDate = (request.FromDate ?? DateTime.UtcNow.AddDays(-60)).Date;
            var toDate   = (request.ToDate   ?? DateTime.UtcNow.AddDays(30)).Date;

            // Mevcut Open sorunları yükle (Acknowledged olanlara dokunulmayacak)
            var existingOpen = await _context.ReconciliationIssues
                .Where(i => i.Status == ReconciliationStatus.Open)
                .Select(i => new { i.Id, i.IssueKey })
                .ToListAsync(ct);
            var existingOpenMap = existingOpen.ToDictionary(i => i.IssueKey, i => i.Id);

            var findings = new List<ReconciliationIssue>();

            // ─── Check 1: ISS qty mismatch ────────────────────────────────────────────
            // ShipmentLine.OrderedQty ≠ IssOrderLine.OrderedQty (kopya bozulmuş)
            var issQtyMismatches = await _context.ShipmentLines
                .Where(sl => sl.IssOrderLineId != null
                          && sl.Shipment.Status != ShipmentStatus.Cancelled
                          && sl.Shipment.Status != ShipmentStatus.Passive
                          && sl.Shipment.DeliveryDate >= fromDate
                          && sl.Shipment.DeliveryDate <= toDate
                          && sl.OrderedQty != sl.IssOrderLine!.OrderedQty)
                .Select(sl => new
                {
                    sl.Id,
                    sl.ShipmentId,
                    sl.StockName,
                    ShipmentQty = sl.OrderedQty,
                    IssQty      = sl.IssOrderLine!.OrderedQty,
                })
                .ToListAsync(ct);

            foreach (var m in issQtyMismatches)
            {
                findings.Add(new ReconciliationIssue
                {
                    IssueKey      = $"IssQtyMismatch:SL{m.Id}",
                    CheckType     = ReconciliationCheckType.IssQtyMismatch,
                    Severity      = ReconciliationSeverity.Warning,
                    Status        = ReconciliationStatus.Open,
                    ShipmentId    = m.ShipmentId,
                    ShipmentLineId = m.Id,
                    Description   = $"ISS sipariş miktarı ile sevkiyat kopyası farklı: '{m.StockName}'",
                    ExpectedValue = m.IssQty.ToString("0.##"),
                    ActualValue   = m.ShipmentQty.ToString("0.##"),
                    DetectedAt    = DateTime.UtcNow,
                });
            }

            // ─── Check 2: Picking tamamlanmamış (Delivered sevkiyatta sıfır satır) ───
            var pickingIncomplete = await _context.ShipmentLines
                .Where(sl => sl.Shipment.Status == ShipmentStatus.Delivered
                          && sl.OrderedQty > 0
                          && sl.DeliveredQty == 0
                          && (sl.ReturnedQty == null || sl.ReturnedQty == 0)
                          && sl.Shipment.DeliveryDate >= fromDate
                          && sl.Shipment.DeliveryDate <= toDate)
                .Select(sl => new { sl.Id, sl.ShipmentId, sl.StockName, sl.OrderedQty })
                .ToListAsync(ct);

            foreach (var p in pickingIncomplete)
            {
                findings.Add(new ReconciliationIssue
                {
                    IssueKey       = $"PickingIncomplete:SL{p.Id}",
                    CheckType      = ReconciliationCheckType.PickingIncomplete,
                    Severity       = ReconciliationSeverity.Error,
                    Status         = ReconciliationStatus.Open,
                    ShipmentId     = p.ShipmentId,
                    ShipmentLineId = p.Id,
                    Description    = $"Teslim edilen sevkiyatta toplanmamış satır: '{p.StockName}'",
                    ExpectedValue  = $"DeliveredQty > 0 (Sipariş: {p.OrderedQty:0.##})",
                    ActualValue    = "0",
                    DetectedAt     = DateTime.UtcNow,
                });
            }

            // ─── Check 3: Netsis aktarımı eksik ──────────────────────────────────────
            // Teslim edileli 24+ saat geçmiş ama Netsis'e aktarılmamış
            var netsisGrace = DateTime.UtcNow.AddDays(-1);
            var netsisTransferMissing = await _context.Shipments
                .Where(s => s.Status == ShipmentStatus.Delivered
                         && s.NetsisTransferredAt == null
                         && s.DeliveredAt != null
                         && s.DeliveredAt < netsisGrace
                         && s.DeliveryDate >= fromDate
                         && s.DeliveryDate <= toDate)
                .Select(s => new { s.Id, s.DeliveredAt })
                .ToListAsync(ct);

            foreach (var n in netsisTransferMissing)
            {
                findings.Add(new ReconciliationIssue
                {
                    IssueKey      = $"NetsisTransferMissing:S{n.Id}",
                    CheckType     = ReconciliationCheckType.NetsisTransferMissing,
                    Severity      = ReconciliationSeverity.Warning,
                    Status        = ReconciliationStatus.Open,
                    ShipmentId    = n.Id,
                    Description   = $"Teslim edilen sevkiyat Netsis'e aktarılmamış (Teslim: {n.DeliveredAt:dd.MM.yyyy HH:mm})",
                    ExpectedValue = "NetsisTransferredAt dolu olmalı",
                    ActualValue   = "null",
                    DetectedAt    = DateTime.UtcNow,
                });
            }

            // ─── Check 4: İrsaliye no eksik ──────────────────────────────────────────
            // Netsis'e aktarılalı 24+ saat geçmiş ama IrsaliyeNo gelmemiş
            var irsaliyeGrace = DateTime.UtcNow.AddHours(-24);
            var irsaliyeMissing = await _context.Shipments
                .Where(s => s.NetsisTransferredAt != null
                         && s.IrsaliyeNo == null
                         && s.NetsisTransferredAt < irsaliyeGrace
                         && s.DeliveryDate >= fromDate
                         && s.DeliveryDate <= toDate)
                .Select(s => new { s.Id, s.NetsisTransferredAt })
                .ToListAsync(ct);

            foreach (var i in irsaliyeMissing)
            {
                findings.Add(new ReconciliationIssue
                {
                    IssueKey      = $"IrsaliyeMissing:S{i.Id}",
                    CheckType     = ReconciliationCheckType.IrsaliyeMissing,
                    Severity      = ReconciliationSeverity.Warning,
                    Status        = ReconciliationStatus.Open,
                    ShipmentId    = i.Id,
                    Description   = $"Netsis'e aktarıldı ancak irsaliye no gelmedi (Aktarım: {i.NetsisTransferredAt:dd.MM.yyyy HH:mm})",
                    ExpectedValue = "IrsaliyeNo dolu olmalı",
                    ActualValue   = "null",
                    DetectedAt    = DateTime.UtcNow,
                });
            }

            // ─── Check 5: ISS satır karşılıksız ──────────────────────────────────────
            // IsTransferred=true olan siparişlerde aktif ShipmentLine olmayan satırlar
            var coveredLineIds = await _context.ShipmentLines
                .Where(sl => sl.IssOrderLineId != null
                          && sl.Shipment.Status != ShipmentStatus.Cancelled
                          && sl.Shipment.DeliveryDate >= fromDate
                          && sl.Shipment.DeliveryDate <= toDate)
                .Select(sl => sl.IssOrderLineId!.Value)
                .Distinct()
                .ToListAsync(ct);

            var uncoveredLines = await _context.IssOrderLines
                .Where(iol => iol.IssOrder.IsTransferred
                           && iol.IssOrder.Status != IssOrderStatus.Cancelled
                           && !coveredLineIds.Contains(iol.Id)
                           && iol.IssOrder.DeliveryDate >= fromDate
                           && iol.IssOrder.DeliveryDate <= toDate)
                .Select(iol => new { iol.Id, iol.IssOrderId, iol.StockName, iol.OrderedQty })
                .ToListAsync(ct);

            foreach (var u in uncoveredLines)
            {
                findings.Add(new ReconciliationIssue
                {
                    IssueKey       = $"IssCoverageGap:IOL{u.Id}",
                    CheckType      = ReconciliationCheckType.IssCoverageGap,
                    Severity       = ReconciliationSeverity.Error,
                    Status         = ReconciliationStatus.Open,
                    IssOrderLineId = u.Id,
                    Description    = $"ISS sipariş satırına karşılık gelen aktif sevkiyat satırı yok: '{u.StockName}'",
                    ExpectedValue  = "Sevkiyat satırı mevcut olmalı",
                    ActualValue    = "Bulunamadı",
                    DetectedAt     = DateTime.UtcNow,
                });
            }

            // ─── Upsert ───────────────────────────────────────────────────────────────
            var currentKeys = findings.Select(f => f.IssueKey).ToHashSet();

            // Artık geçersiz Open sorunları AutoResolved yap
            var toAutoResolveKeys = existingOpenMap.Keys.Where(k => !currentKeys.Contains(k)).ToList();
            int autoResolved = 0;
            if (toAutoResolveKeys.Any())
            {
                var resolveIds = toAutoResolveKeys.Select(k => existingOpenMap[k]).ToList();
                var toResolve = await _context.ReconciliationIssues
                    .Where(i => resolveIds.Contains(i.Id))
                    .ToListAsync(ct);
                foreach (var issue in toResolve)
                    issue.Status = ReconciliationStatus.AutoResolved;
                autoResolved = toResolve.Count;
            }

            // Yeni bulunan sorunları kaydet (zaten Open olanları tekrar ekleme)
            var newIssues = findings.Where(f => !existingOpenMap.ContainsKey(f.IssueKey)).ToList();
            if (newIssues.Any())
                _context.ReconciliationIssues.AddRange(newIssues);

            await _context.SaveChangesAsync(ct);

            var totalOpen = await _context.ReconciliationIssues
                .CountAsync(i => i.Status == ReconciliationStatus.Open, ct);

            return new RunReconciliationChecksResult
            {
                NewIssuesFound = newIssues.Count,
                AutoResolved   = autoResolved,
                TotalOpenAfter = totalOpen,
                ByCheckType    = newIssues
                    .GroupBy(i => i.CheckType.ToString())
                    .ToDictionary(g => g.Key, g => g.Count()),
            };
        }
    }
}
