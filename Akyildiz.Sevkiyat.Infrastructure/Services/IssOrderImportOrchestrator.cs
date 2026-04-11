using System.Globalization;
using System.Text.Json;
using Akyildiz.Sevkiyat.Application.Common;
using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.External.IssIp.Dtos;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Akyildiz.Sevkiyat.Infrastructure.Services;

public class IssOrderImportOrchestrator : IIssOrderImportOrchestrator
{
    private readonly IISSIpClient _issClient;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<IssOrderImportOrchestrator> _logger;

    public IssOrderImportOrchestrator(
        IISSIpClient issClient,
        IApplicationDbContext context,
        ILogger<IssOrderImportOrchestrator> logger)
    {
        _issClient = issClient;
        _context = context;
        _logger = logger;
    }

    public async Task<IssOrderImportResult> RunAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
        => await RunCoreAsync(null, start, end, cancellationToken);

    public async Task<IssOrderImportResult> RunAsync(int existingBatchId, DateTime start, DateTime end, CancellationToken cancellationToken = default)
        => await RunCoreAsync(existingBatchId, start, end, cancellationToken);

    private async Task<IssOrderImportResult> RunCoreAsync(int? existingBatchId, DateTime start, DateTime end, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        int added = 0;
        int skipped = 0;
        int errors = 0;
        int needsMapping = 0;

        // Create or load ImportBatch record
        Domain.Entities.ImportBatch batch;
        if (existingBatchId.HasValue)
        {
            batch = await _context.ImportBatches.FindAsync(new object[] { existingBatchId.Value }, cancellationToken)
                ?? throw new InvalidOperationException($"ImportBatch {existingBatchId} not found");
            batch.StartedAt = DateTime.UtcNow; // refresh timestamp
        }
        else
        {
            batch = new Domain.Entities.ImportBatch
            {
                RequestedStartDate = start,
                RequestedEndDate = end,
                StartedAt = DateTime.UtcNow,
                Status = ImportBatchStatus.Running
            };
            _context.ImportBatches.Add(batch);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var batchErrors = new List<string>();

        try
        {
            // 1. Fetch Order List
            var listEnvelope = await _issClient.GetSiparisListesiAsync(start, end, cancellationToken);
            var listRoot = GetRefinedRoot(listEnvelope.Root);

            string rawList = listRoot.GetRawText();
            _logger.LogInformation("Refined List Root (First 500): {RawListSnippet}", rawList.Substring(0, Math.Min(500, rawList.Length)));

            List<IssSiparisHeaderDto>? orderList = null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            try
            {
                if (listRoot.ValueKind == JsonValueKind.Array)
                {
                    orderList = JsonSerializer.Deserialize<List<IssSiparisHeaderDto>>(listRoot.GetRawText(), opts);
                }
                else if (listRoot.ValueKind == JsonValueKind.Object)
                {
                    if (TryGetProperty(listRoot, out var arrayProp, "Table", "SiparisListesi", "Result", "Data", "Value"))
                    {
                        orderList = JsonSerializer.Deserialize<List<IssSiparisHeaderDto>>(arrayProp.GetRawText(), opts);
                    }
                    else
                    {
                        throw new DomainException("Unexpected JSON structure for Order List. No 'Table' property found.");
                    }
                }
                else
                {
                    throw new DomainException($"Unexpected JSON root kind: {listRoot.ValueKind}.");
                }
            }
            catch (Exception ex) when (ex is not DomainException)
            {
                _logger.LogError(ex, "LIST PARSE ERROR");
                throw;
            }

            if (orderList == null || !orderList.Any())
            {
                batch.Status = ImportBatchStatus.Completed;
                batch.CompletedAt = DateTime.UtcNow;
                batch.DurationMs = (int)sw.ElapsedMilliseconds;
                await _context.SaveChangesAsync(cancellationToken);
                return new IssOrderImportResult(0, 0, 0, 0, 0, batch.Id, new List<string>());
            }

            batch.TotalFromSource = orderList.Count;

            // 2. Load caches — tek seferde, döngü öncesi
            var knownMappings = await _context.StockMappings
               .Where(m => m.ExternalSystem == "ISS-IP")
               .ToDictionaryAsync(m => m.ExternalStockCode, m => m, cancellationToken);

            var allStocks = await _context.StockMasters
                .Where(s => s.IsActive)
                .Select(s => new { s.Id, s.StockName })
                .ToListAsync(cancellationToken);
            var stockNameLookup = allStocks
                .GroupBy(s => TextNormalizer.NormalizeForComparison(s.StockName))
                .Where(g => g.Count() == 1)
                .ToDictionary(g => g.Key, g => g.First().Id);

            // Mevcut sipariş numaralarını tek sorguda çek — döngü içi N+1 önleme
            var incomingOrderNumbers = orderList
                .Where(h => !string.IsNullOrEmpty(h.SiparisNo))
                .Select(h => h.SiparisNo!)
                .ToList();

            var existingOrderNumberMap = await _context.IssOrders
                .Where(o => incomingOrderNumbers.Contains(o.ExternalOrderNumber))
                .Select(o => new { o.ExternalOrderNumber, o.Id })
                .ToDictionaryAsync(o => o.ExternalOrderNumber, o => o.Id, cancellationToken);

            // Mevcut projeleri tek sorguda çek
            var allProjects = await _context.Projects
                .ToDictionaryAsync(p => p.Code, p => p, StringComparer.OrdinalIgnoreCase, cancellationToken);

            var batchProjectCache = new Dictionary<string, Domain.Entities.Project>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in allProjects) batchProjectCache[kv.Key] = kv.Value;

            // 3. Process each order with per-order isolation
            foreach (var header in orderList)
            {
                if (string.IsNullOrEmpty(header.SiparisNo)) continue;

                var batchOrder = new Domain.Entities.ImportBatchOrder
                {
                    ImportBatchId = batch.Id,
                    ExternalOrderNumber = header.SiparisNo
                };

                try
                {
                    // Check existence — ISS orders are immutable, skip if already imported
                    if (existingOrderNumberMap.TryGetValue(header.SiparisNo, out var existingId))
                    {
                        batchOrder.Action = "Skipped";
                        batchOrder.IssOrderId = existingId;
                        skipped++;
                        _context.ImportBatchOrders.Add(batchOrder);
                        continue;
                    }

                    // Fetch Detail (new orders only)
                    var detailEnvelope = await _issClient.GetSiparisAsync(header.SiparisNo, cancellationToken);
                    var detailRoot = GetRefinedRoot(detailEnvelope.Root);

                    IssSiparisDetailDto detail = new IssSiparisDetailDto();

                    if (detailRoot.ValueKind == JsonValueKind.Object)
                    {
                        if (TryGetProperty(detailRoot, out var table1, "Table1", "Lines", "Satirlar", "lines"))
                            detail.Lines = JsonSerializer.Deserialize<List<IssSiparisLineDto>>(table1.GetRawText(), opts) ?? new List<IssSiparisLineDto>();

                        if (TryGetProperty(detailRoot, out var table0, "Table", "Header"))
                        {
                            var headers = JsonSerializer.Deserialize<List<IssSiparisHeaderDto>>(table0.GetRawText(), opts);
                            if (headers != null && headers.Any())
                                detail.Header = headers.First();
                        }
                    }
                    else if (detailRoot.ValueKind == JsonValueKind.Array)
                    {
                        detail.Lines = JsonSerializer.Deserialize<List<IssSiparisLineDto>>(detailRoot.GetRawText(), opts) ?? new List<IssSiparisLineDto>();
                    }

                    if (detail.Lines == null) detail.Lines = new List<IssSiparisLineDto>();

                    // Validate: reject empty-line orders
                    if (!detail.Lines.Any())
                    {
                        _logger.LogWarning("Order {OrderNumber} has no lines — rejected.", header.SiparisNo);
                        batchOrder.Action = "Failed";
                        batchOrder.Error = "Sipariş satırı bulunamadı";
                        errors++;
                        batchErrors.Add($"{header.SiparisNo}: Satır bulunamadı");
                        _context.ImportBatchOrders.Add(batchOrder);
                        continue;
                    }

                    // Parse dates
                    DateTime orderDate    = ParseDate(detail.Header?.Tarih ?? header.Tarih) ?? DateTime.Now;
                    DateTime deliveryDate = ParseDate(detail.Header?.TeslimTarihi ?? header.TeslimTarihi) ?? DateTime.Now.AddDays(1);
                    string? kurumKodu  = detail.Header?.KurumKodu ?? header.KurumKodu;
                    string? projeKodu  = detail.Header?.ProjeKodu ?? header.ProjeKodu;
                    string projectCodeToUse = !string.IsNullOrEmpty(projeKodu) ? projeKodu : (kurumKodu ?? "UNKNOWN");

                    // Find or create project
                    Domain.Entities.Project? projectEntity = null;
                    if (!batchProjectCache.TryGetValue(projectCodeToUse, out projectEntity))
                        projectEntity = await _context.Projects.FirstOrDefaultAsync(p => p.Code == projectCodeToUse, cancellationToken);

                    if (projectEntity == null)
                    {
                        projectEntity = new Domain.Entities.Project
                        {
                            Code = projectCodeToUse,
                            Name = "Bilinmiyor " + projectCodeToUse,
                            InstitutionCode = kurumKodu,
                            IsActive = true,
                            LastSyncedAt = null
                        };
                        _context.Projects.Add(projectEntity);
                        batchProjectCache[projectCodeToUse] = projectEntity;
                    }

                    var newOrder = new Domain.Entities.IssOrder
                    {
                        ExternalOrderNumber = header.SiparisNo,
                        OrderDate = orderDate,
                        DeliveryDate = deliveryDate,
                        Status = IssOrderStatus.Imported,
                        ImportStatus = ImportStatus.Ready,
                        TalepNo = detail.Header?.TalepNo ?? header.TalepNo,
                        TeslimAlacakKisiler = detail.Header?.TeslimAlacakKisiler ?? header.TeslimAlacakKisiler,
                        TeslimAlacakTelefonNumaralari = detail.Header?.TeslimAlacakTelefonNumaralari ?? header.TeslimAlacakTelefonNumaralari,
                        TalepTuru = detail.Header?.TalepTuru ?? header.TalepTuru,
                        Aciklama = detail.Header?.Aciklama ?? header.Aciklama,
                        Donem = detail.Header?.Donem ?? header.Donem,
                        YoneticiMailAdresleri = detail.Header?.YoneticiMailAdresleri ?? header.YoneticiMailAdresleri,
                        IsActive = true,
                        Project = projectEntity
                    };

                    bool hasUnmapped = false;

                    foreach (var line in detail.Lines)
                    {
                        if (string.IsNullOrEmpty(line.MalzemeKodu)) continue;

                        var issLine = new Domain.Entities.IssOrderLine
                        {
                            LineNumber = 0,
                            StockCode = line.MalzemeKodu,
                            StockName = line.MalzemeAdi ?? "Unknown",
                            Unit = ConvertToStockUnit(line.Birimi),
                            OrderedQty = line.Miktari ?? 0,
                            ListeFiyati = line.ListeFiyati,
                            Iskonto = line.Iskonto,
                            BirimFiyati = line.BirimFiyati,
                            KDVOrani = line.KDVOrani
                        };

                        if (knownMappings.TryGetValue(line.MalzemeKodu, out var mapping))
                        {
                            if (mapping.MatchStatus == MatchStatus.Ignored) hasUnmapped = true;
                            else if (mapping.MatchStatus == MatchStatus.Unmapped || mapping.LocalStockId == null) hasUnmapped = true;
                        }
                        else
                        {
                            var newMap = new Domain.Entities.StockMapping
                            {
                                ExternalSystem = "ISS-IP",
                                ExternalStockCode = line.MalzemeKodu,
                                ExternalStockName = line.MalzemeAdi ?? "Unknown",
                                MatchStatus = MatchStatus.Unmapped
                            };

                            var nameKey = TextNormalizer.NormalizeForComparison(line.MalzemeAdi);
                            if (!string.IsNullOrEmpty(nameKey) && stockNameLookup.TryGetValue(nameKey, out var autoMatchedId))
                            {
                                newMap.LocalStockId = autoMatchedId;
                                newMap.MatchStatus = MatchStatus.Mapped;
                            }
                            else
                            {
                                hasUnmapped = true;
                            }

                            _context.StockMappings.Add(newMap);
                            knownMappings[line.MalzemeKodu] = newMap;
                        }

                        newOrder.Lines.Add(issLine);
                    }

                    newOrder.ImportStatus = hasUnmapped ? ImportStatus.NeedsMapping : ImportStatus.Ready;
                    _context.IssOrders.Add(newOrder);

                    batchOrder.Action = "Created";
                    batchOrder.IssOrderId = newOrder.Id; // Will be set after SaveChanges

                    if (hasUnmapped)
                    {
                        batchOrder.Warning = "Stok eşleştirmesi gerekiyor";
                        needsMapping++;
                    }

                    added++;
                    _context.ImportBatchOrders.Add(batchOrder);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Order import failed: {OrderNumber}", header.SiparisNo);
                    batchOrder.Action = "Failed";
                    batchOrder.Error = ex.Message[..Math.Min(500, ex.Message.Length)];
                    errors++;
                    batchErrors.Add($"{header.SiparisNo}: {ex.Message[..Math.Min(200, ex.Message.Length)]}");
                    _context.ImportBatchOrders.Add(batchOrder);
                    // Continue — don't let one bad order kill the batch
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Update batch
            batch.NewCount = added;
            batch.SkippedCount = skipped;
            batch.NeedsMappingCount = needsMapping;
            batch.FailedCount = errors;
            batch.Status = errors == 0
                ? ImportBatchStatus.Completed
                : (added > 0 ? ImportBatchStatus.PartialSuccess : ImportBatchStatus.Failed);
            batch.CompletedAt = DateTime.UtcNow;
            batch.DurationMs = (int)sw.ElapsedMilliseconds;
            if (batchErrors.Any())
                batch.ErrorSummary = string.Join("; ", batchErrors.Take(5));

            await _context.SaveChangesAsync(cancellationToken);

            return new IssOrderImportResult(
                TotalFromSource: batch.TotalFromSource,
                Added: added,
                Skipped: skipped,
                NeedsMapping: needsMapping,
                Errors: errors,
                BatchId: batch.Id,
                ErrorMessages: batchErrors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GLOBAL IMPORT ERROR (BatchId={BatchId})", batch.Id);
            batch.Status = ImportBatchStatus.Failed;
            batch.ErrorSummary = ex.Message[..Math.Min(500, ex.Message.Length)];
            batch.CompletedAt = DateTime.UtcNow;
            batch.DurationMs = (int)sw.ElapsedMilliseconds;
            try { await _context.SaveChangesAsync(cancellationToken); } catch { /* best-effort */ }
            throw;
        }
    }

    private DateTime? ParseDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr)) return null;
        if (DateTime.TryParseExact(dateStr, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d1)) return d1;
        if (DateTime.TryParse(dateStr, out var d2)) return d2;
        return null;
    }

    private JsonElement GetRefinedRoot(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            if (TryGetProperty(root, out var resProp, "result", "Result") && resProp.ValueKind == JsonValueKind.String)
            {
                try
                {
                    var innerJson = resProp.GetString();
                    if (!string.IsNullOrWhiteSpace(innerJson))
                    {
                        var doc = JsonDocument.Parse(innerJson);
                        return doc.RootElement;
                    }
                }
                catch { }
            }
        }
        return root;
    }

    private bool TryGetProperty(JsonElement element, out JsonElement value, params string[] propertyNames)
    {
        value = default;
        foreach (var name in propertyNames)
            if (element.TryGetProperty(name, out value)) return true;
        return false;
    }

    private StockUnit ConvertToStockUnit(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return StockUnit.Adet;
        return v.ToUpper().Replace("İ", "I") switch
        {
            "ADET" or "AD" or "AD." => StockUnit.Adet,
            "KG" or "KILOGRAM" or "KİLOGARAM" => StockUnit.Kg,
            "PAKET" or "PK" or "PK." => StockUnit.Paket,
            "KOLI" or "KOLİ" or "KL" or "KL." => StockUnit.Koli,
            "LITRE" or "LT" or "LT." => StockUnit.Litre,
            "METRE" or "MT" or "MT." => StockUnit.Metre,
            "METREKARE" or "M2" => StockUnit.Metrekare,
            "SET" => StockUnit.Set,
            "TENEKE" => StockUnit.Teneke,
            _ => Enum.TryParse<StockUnit>(v, true, out var result) ? result : StockUnit.Diger
        };
    }
}
