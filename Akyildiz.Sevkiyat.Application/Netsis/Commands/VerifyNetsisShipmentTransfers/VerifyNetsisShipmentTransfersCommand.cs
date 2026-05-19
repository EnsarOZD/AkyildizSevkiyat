using Akyildiz.Sevkiyat.Application.External.Netsis.Dtos;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Akyildiz.Sevkiyat.Application.Netsis.Commands.VerifyNetsisShipmentTransfers
{
    /// <summary>
    /// Netsis'e aktarılmış görünen ama Netsis tarafından silinmiş olabilecek
    /// sevkiyatları kontrol eder.
    /// - Delivered durumundaki sevkiyatlarda irsaliye silinmişse otomatik stok iadesi yapılır
    ///   ve sevkiyat ReturnedToWarehouse'a alınır.
    /// - Diğer durumlarda NetsisTransferredAt sıfırlanır; sevkiyat tekrar "Netsis'e Aktar" ile gönderilebilir.
    /// </summary>
    public record VerifyNetsisShipmentTransfersCommand : IRequest<VerifyNetsisShipmentTransfersResult>
    {
        public DateTime? StartDate { get; init; }
        public DateTime? EndDate { get; init; }
        public int? ZoneId { get; init; }
        public ShipmentStatus? Status { get; init; }
        public string? Statuses { get; init; }
        public string? Search { get; init; }
    }

    public class VerifyNetsisShipmentTransfersResult
    {
        public int Checked           { get; set; }
        public int Reset             { get; set; }   // Netsis'te hiç bulunamayan → sıfırlanan
        public int AutoReturnCreated { get; set; }   // Delivered + silinmiş irsaliye → otomatik iade + stok girişi
        public int StillOk           { get; set; }   // Netsis'te bizim aktarımımız olarak mevcut
        public int Foreign           { get; set; }   // Netsis'te var ama başka sistem aktarmış — dokunulmadı
        public int AutoDetected      { get; set; }   // Aktarılmamış ama Netsis'te bulundu → otomatik işaretlendi
        public int IrsaliyesSynced   { get; set; }   // StillOk içinden IrsaliyeNo güncellenenler
        public string? Error         { get; set; }
    }

    public class VerifyNetsisShipmentTransfersCommandHandler
        : IRequestHandler<VerifyNetsisShipmentTransfersCommand, VerifyNetsisShipmentTransfersResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly INetsisClient _netsis;
        private readonly ILogger<VerifyNetsisShipmentTransfersCommandHandler> _logger;

        public VerifyNetsisShipmentTransfersCommandHandler(
            IApplicationDbContext context,
            INetsisClient netsis,
            ILogger<VerifyNetsisShipmentTransfersCommandHandler> logger)
        {
            _context = context;
            _netsis  = netsis;
            _logger  = logger;
        }

        public async Task<VerifyNetsisShipmentTransfersResult> Handle(
            VerifyNetsisShipmentTransfersCommand request,
            CancellationToken cancellationToken)
        {
            var query = _context.Shipments
                .Include(s => s.Project)
                .Include(s => s.IssOrder)
                .Where(s => s.IssOrder != null);

            if (request.StartDate.HasValue)
                query = query.Where(s => s.DeliveryDate.Date >= request.StartDate.Value.Date);
            if (request.EndDate.HasValue)
                query = query.Where(s => s.DeliveryDate.Date <= request.EndDate.Value.Date);
            if (request.ZoneId.HasValue)
                query = query.Where(s => s.Project.ZoneId == request.ZoneId.Value);

            if (request.Status.HasValue)
            {
                query = query.Where(s => s.Status == request.Status.Value);
            }
            else if (!string.IsNullOrEmpty(request.Statuses))
            {
                var statuses = request.Statuses.Split(',').Select(x => Enum.Parse<ShipmentStatus>(x)).ToList();
                query = query.Where(s => statuses.Contains(s.Status));
            }

            if (!string.IsNullOrEmpty(request.Search))
            {
                var search = request.Search.ToLower();
                query = query.Where(s =>
                    s.Project.Code.ToLower().Contains(search) ||
                    s.Project.Name.ToLower().Contains(search) ||
                    (s.IssOrder!.ExternalOrderNumber != null && s.IssOrder.ExternalOrderNumber.ToLower().Contains(search)) ||
                    (s.IssOrder!.TalepNo != null && s.IssOrder.TalepNo.ToLower().Contains(search)) ||
                    (s.IrsaliyeNo != null && s.IrsaliyeNo.ToLower().Contains(search)) ||
                    s.Id.ToString() == search);
            }

            var shipmentSummaries = await query
                .Where(s => s.NetsisTransferredAt.HasValue && s.IssOrder!.NetsisOrderNumber != null)
                .Select(s => new
                {
                    s.Id,
                    s.Status,
                    NetsisOrderNumber = s.IssOrder!.NetsisOrderNumber!,
                })
                .ToListAsync(cancellationToken);

            if (!shipmentSummaries.Any())
            {
                var emptyResult = new VerifyNetsisShipmentTransfersResult();
                emptyResult.AutoDetected = await DetectAlreadyInNetsisAsync(query, cancellationToken);
                return emptyResult;
            }

            _logger.LogInformation(
                "VerifyNetsisTransfers: {Count} aktarılmış sevkiyat kontrol ediliyor.", shipmentSummaries.Count);

            var (missing, foreign, netsisError) = await _netsis.CheckShipmentOrdersMissingAsync(
                shipmentSummaries.Select(s => (s.NetsisOrderNumber, s.Id.ToString())),
                cancellationToken);

            if (netsisError != null)
                _logger.LogWarning("VerifyNetsisTransfers: Netsis hatası — {Error}", netsisError);

            var result = new VerifyNetsisShipmentTransfersResult
            {
                Checked = shipmentSummaries.Count,
                Foreign = foreign.Count,
                Error   = netsisError,
            };

            if (foreign.Any())
                _logger.LogWarning(
                    "VerifyNetsisTransfers: {Count} sevkiyat başka bir sistem tarafından Netsis'e aktarılmış " +
                    "(EKACK1 eşleşmedi). NetsisTransferredAt sıfırlanmadı — sipariş Netsis'te mevcut.",
                    foreign.Count);

            if (missing.Any())
            {
                var missingIds = shipmentSummaries
                    .Where(s => missing.Contains(s.NetsisOrderNumber))
                    .Select(s => s.Id)
                    .ToHashSet();

                var deliveredMissingIds = shipmentSummaries
                    .Where(s => missing.Contains(s.NetsisOrderNumber) && s.Status == ShipmentStatus.Delivered)
                    .Select(s => s.Id)
                    .ToHashSet();

                var nonDeliveredMissingIds = missingIds.Except(deliveredMissingIds).ToHashSet();

                // ── Delivered + silinmiş irsaliye: otomatik stok iadesi ─────────────
                if (deliveredMissingIds.Any())
                {
                    var toAutoReturn = await _context.Shipments
                        .Include(s => s.IssOrder)
                        .Include(s => s.Lines)
                        .Where(s => deliveredMissingIds.Contains(s.Id))
                        .ToListAsync(cancellationToken);

                    var stockMasterIds = toAutoReturn
                        .SelectMany(s => s.Lines)
                        .Where(l => l.StockMasterId.HasValue)
                        .Select(l => l.StockMasterId!.Value)
                        .Distinct()
                        .ToList();

                    var stockMap = stockMasterIds.Count > 0
                        ? (await _context.StockMasters
                            .Where(s => stockMasterIds.Contains(s.Id))
                            .ToListAsync(cancellationToken))
                            .ToDictionary(s => s.Id)
                        : new Dictionary<int, StockMaster>();

                    var now = DateTime.UtcNow;

                    foreach (var shipment in toAutoReturn)
                    {
                        _logger.LogWarning(
                            "VerifyNetsisTransfers: Sevkiyat #{Id} (Delivered) — Netsis irsaliyesi silinmiş, otomatik iade kaydı oluşturuluyor.",
                            shipment.Id);

                        // Stok girişi: her satır için teslim edilen miktar kadar iade
                        foreach (var line in shipment.Lines)
                        {
                            var returnQty = line.DeliveredQty > 0 ? line.DeliveredQty : line.OrderedQty;
                            if (returnQty <= 0) continue;

                            line.RecordReturn((line.ReturnedQty ?? 0) + returnQty,
                                ReturnReason.Other);

                            if (line.StockMasterId.HasValue
                                && stockMap.TryGetValue(line.StockMasterId.Value, out var stock))
                            {
                                stock.Increase(returnQty);

                                _context.StockTransactions.Add(new StockTransaction
                                {
                                    StockMasterId   = stock.Id,
                                    Type            = StockTransactionType.VehicleReturn,
                                    Qty             = returnQty,
                                    Reference       = $"SHP-{shipment.Id}",
                                    Date            = now,
                                    Note            = $"Sevkiyat #{shipment.Id} — Netsis irsaliyesi silindi, otomatik stok iadesi"
                                });
                            }
                        }

                        shipment.RevertNetsisTransfer();
                        shipment.RecordVehicleReturn(now, "Netsis irsaliyesi silindi — otomatik iade");
                        shipment.ChangeStatus(ShipmentStatus.ReturnedToWarehouse, null,
                            "Netsis irsaliyesi silindi — sistem tarafından otomatik depoya iade");

                        if (shipment.IssOrder != null)
                        {
                            shipment.IssOrder.NetsisOrderNumber = null;
                            shipment.IssOrder.IsTransferred = false;
                        }
                    }

                    result.AutoReturnCreated = toAutoReturn.Count;
                }

                // ── Delivered olmayan + silinmiş irsaliye: sadece aktarım sıfırla ───
                if (nonDeliveredMissingIds.Any())
                {
                    var toReset = await _context.Shipments
                        .Include(s => s.IssOrder)
                        .Where(s => nonDeliveredMissingIds.Contains(s.Id))
                        .ToListAsync(cancellationToken);

                    foreach (var shipment in toReset)
                    {
                        _logger.LogWarning(
                            "VerifyNetsisTransfers: Sevkiyat #{Id} ({Status}) — Netsis'te bulunamadı, aktarım sıfırlanıyor.",
                            shipment.Id, shipment.Status);

                        shipment.RevertNetsisTransfer();

                        if (shipment.IssOrder != null)
                        {
                            shipment.IssOrder.NetsisOrderNumber = null;
                            shipment.IssOrder.IsTransferred = false;
                        }
                    }

                    result.Reset = toReset.Count;
                }

                await _context.SaveChangesAsync(cancellationToken);
                result.StillOk = shipmentSummaries.Count - missingIds.Count - foreign.Count;
            }
            else
            {
                result.StillOk = shipmentSummaries.Count - foreign.Count;
            }

            // StillOk sevkiyatlarda IrsaliyeNo boşsa Netsis'ten çek.
            // Sipariş numarası (NetsisOrderNumber) ile irsaliye numarası farklı tablolarda — GetIrsaliyelerAsync
            // TBLSTHAR'dan gerçek FISNO'yu (irsaliye no) döndürür.
            var stillOkIds = shipmentSummaries
                .Where(s => !missing.Contains(s.NetsisOrderNumber, StringComparer.OrdinalIgnoreCase)
                         && !foreign.Contains(s.NetsisOrderNumber, StringComparer.OrdinalIgnoreCase))
                .Select(s => s.Id)
                .ToHashSet();
            result.IrsaliyesSynced = await SyncMissingIrsaliyeAsync(stillOkIds, cancellationToken);

            result.AutoDetected = await DetectAlreadyInNetsisAsync(query, cancellationToken);

            return result;
        }

        private async Task<int> SyncMissingIrsaliyeAsync(
            IReadOnlySet<int> stillOkIds,
            CancellationToken cancellationToken)
        {
            if (!stillOkIds.Any()) return 0;

            var needIrsaliye = await _context.Shipments
                .Include(s => s.IssOrder)
                .Where(s => stillOkIds.Contains(s.Id)
                         && s.IrsaliyeNo == null
                         && s.IssOrder != null
                         && s.IssOrder.NetsisOrderNumber != null)
                .ToListAsync(cancellationToken);

            if (!needIrsaliye.Any()) return 0;

            _logger.LogInformation(
                "VerifyNetsisTransfers: {Count} StillOk sevkiyatta IrsaliyeNo eksik, Netsis'ten çekiliyor.",
                needIrsaliye.Count);

            int synced = 0;
            foreach (var shipment in needIrsaliye)
            {
                try
                {
                    var irsaliyeler = await _netsis.GetIrsaliyelerAsync(
                        new NetsisIrsaliyeQuery { SiparisNo = shipment.IssOrder!.NetsisOrderNumber },
                        cancellationToken);

                    if (irsaliyeler?.Any() == true)
                    {
                        var ilk = irsaliyeler.First();
                        shipment.SetIrsaliyeInfo(ilk.IrsaliyeNo, ilk.IrsaliyeTarihi);
                        _logger.LogInformation(
                            "VerifyNetsisTransfers: Sevkiyat #{Id} → IrsaliyeNo={No} güncellendi.",
                            shipment.Id, ilk.IrsaliyeNo);
                        synced++;
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "VerifyNetsisTransfers: Sevkiyat #{Id} irsaliye çekimi başarısız — atlanıyor.", shipment.Id);
                }
            }

            if (synced > 0)
                await _context.SaveChangesAsync(cancellationToken);

            return synced;
        }

        private async Task<int> DetectAlreadyInNetsisAsync(IQueryable<Shipment> baseQuery, CancellationToken cancellationToken)
        {
            var exportableStatuses = new[]
            {
                ShipmentStatus.ReadyForDispatch,
                ShipmentStatus.AssignedToVehicle,
            };

            var candidates = await baseQuery
                .Where(s => !s.NetsisTransferredAt.HasValue
                         && exportableStatuses.Contains(s.Status)
                         && s.IssOrder != null
                         && s.IssOrder.ExternalOrderNumber != null
                         && s.IssOrder.NetsisOrderNumber == null)
                .Select(s => new
                {
                    s.Id,
                    ExternalOrderNumber = s.IssOrder!.ExternalOrderNumber!,
                })
                .ToListAsync(cancellationToken);

            if (!candidates.Any()) return 0;

            _logger.LogInformation(
                "VerifyNetsisTransfers: {Count} aktarılmamış sevkiyat Netsis'te aranıyor.",
                candidates.Count);

            var (found, checkError) = await _netsis.CheckOrdersExistInNetsisAsync(
                candidates.Select(c => c.ExternalOrderNumber),
                cancellationToken);

            if (checkError != null)
                _logger.LogWarning("VerifyNetsisTransfers (auto-detect): Netsis hatası — {Error}", checkError);

            if (!found.Any()) return 0;

            var foundIds = candidates
                .Where(c => found.Contains(c.ExternalOrderNumber))
                .Select(c => c.Id)
                .ToHashSet();

            var toMark = await _context.Shipments
                .Include(s => s.IssOrder)
                .Where(s => foundIds.Contains(s.Id))
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            foreach (var shipment in toMark)
            {
                _logger.LogInformation(
                    "VerifyNetsisTransfers: Sevkiyat #{Id} — Netsis'te bulundu, otomatik işaretleniyor. ExternalOrderNumber={No}",
                    shipment.Id, shipment.IssOrder?.ExternalOrderNumber);

                shipment.MarkNetsisTransferred(now);

                if (shipment.IssOrder != null)
                    shipment.IssOrder.NetsisOrderNumber = shipment.IssOrder.ExternalOrderNumber;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return toMark.Count;
        }
    }
}
