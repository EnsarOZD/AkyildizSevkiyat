using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using ExcelDataReader;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Stocks.Commands.ImportStockMappings
{
    public record ImportStockMappingsCommand(Stream FileStream) : IRequest<ImportStockMappingsResult>;

    public class ImportStockMappingsResult
    {
        public int MappedCount { get; set; }
        public List<string> NotFoundMappings { get; set; } = new();  // External code not in StockMappings
        public List<string> NotFoundStocks { get; set; } = new();    // Local code not in StockMasters
        public List<string> Skipped { get; set; } = new();           // Empty / invalid rows
    }

    public class ImportStockMappingsCommandHandler : IRequestHandler<ImportStockMappingsCommand, ImportStockMappingsResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<ImportStockMappingsCommandHandler> _logger;

        public ImportStockMappingsCommandHandler(IApplicationDbContext context, ILogger<ImportStockMappingsCommandHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ImportStockMappingsResult> Handle(ImportStockMappingsCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("--- Mapping Import Start: {StartDateTime} ---", System.DateTime.UtcNow);

            var result = new ImportStockMappingsResult();

            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                using var reader = ExcelReaderFactory.CreateReader(request.FileStream);

                var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                    {
                        UseHeaderRow = true
                    }
                });

                if (dataSet.Tables.Count == 0)
                {
                    _logger.LogWarning("No tables found in Excel file.");
                    return result;
                }

                var table = dataSet.Tables[0];
                _logger.LogInformation("Table: {TableName}, Rows: {RowCount}, Cols: {ColCount}",
                    table.TableName, table.Rows.Count, table.Columns.Count);

                if (table.Columns.Count < 3)
                {
                    result.Skipped.Add($"Excel dosyasında en az 3 sütun gereklidir (A: ISS Kodu, B: ISS Adı, C: Yerel Stok Kodu). Bulunan sütun sayısı: {table.Columns.Count}");
                    return result;
                }

                // D sütunu (index 3) opsiyonel — Netsis Stok Kodu
                bool hasNetsisCol = table.Columns.Count >= 4;

                var rowsToProcess = new List<(string ExtCode, string LocCode, string? NetsisCode)>();
                foreach (DataRow row in table.Rows)
                {
                    var externalCode = row[0]?.ToString()?.Trim();
                    var localCode    = row[2]?.ToString()?.Trim();
                    var netsisCode   = hasNetsisCol ? row[3]?.ToString()?.Trim() : null;

                    if (string.IsNullOrEmpty(externalCode) && string.IsNullOrEmpty(localCode))
                    {
                        result.Skipped.Add("Boş satır atlandı.");
                        continue;
                    }

                    if (string.IsNullOrEmpty(externalCode))
                    {
                        result.Skipped.Add("A sütunu (ISS Kodu) boş — satır atlandı.");
                        continue;
                    }

                    if (string.IsNullOrEmpty(localCode))
                    {
                        result.Skipped.Add($"'{externalCode}': C sütunu (Yerel Stok Kodu) boş — satır atlandı.");
                        continue;
                    }

                    rowsToProcess.Add((externalCode, localCode, string.IsNullOrWhiteSpace(netsisCode) ? null : netsisCode));
                }

                if (rowsToProcess.Any())
                {
                    _logger.LogInformation("Processing {Count} rows from Excel.", rowsToProcess.Count);

                    var extCodes = rowsToProcess.Select(r => r.ExtCode).Distinct().ToList();
                    var locCodes = rowsToProcess.Select(r => r.LocCode).Distinct().ToList();

                    var existingMappings = await _context.StockMappings
                        .Where(m => extCodes.Contains(m.ExternalStockCode))
                        .ToListAsync(cancellationToken);

                    var mappingsDict = existingMappings
                        .GroupBy(m => m.ExternalStockCode.Trim(), StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                    // StockMasters — NetsisStockCode güncellemesi için tracked olarak yükle
                    var existingLocalStocks = await _context.StockMasters
                        .Where(s => locCodes.Contains(s.StockCode))
                        .ToListAsync(cancellationToken);

                    var localStockDict = existingLocalStocks
                        .GroupBy(s => s.StockCode.Trim(), StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                    foreach (var (externalCode, localCode, netsisCode) in rowsToProcess)
                    {
                        if (!mappingsDict.TryGetValue(externalCode, out var mapping))
                        {
                            result.NotFoundMappings.Add(externalCode);
                            continue;
                        }

                        if (!localStockDict.TryGetValue(localCode, out var localStock))
                        {
                            result.NotFoundStocks.Add($"'{localCode}' (ISS: {externalCode} için)");
                            continue;
                        }

                        bool changed = false;

                        if (mapping.LocalStockId != localStock.Id || mapping.MatchStatus != MatchStatus.Mapped)
                        {
                            mapping.LocalStockId  = localStock.Id;
                            mapping.MatchStatus   = MatchStatus.Mapped;
                            changed = true;
                        }

                        // D sütunu doldurulmuşsa NetsisStockCode güncelle
                        if (netsisCode != null && localStock.NetsisStockCode != netsisCode)
                        {
                            localStock.NetsisStockCode = netsisCode;
                            changed = true;
                        }

                        if (changed) result.MappedCount++;
                    }

                    if (result.MappedCount > 0)
                    {
                        _logger.LogInformation("Saving {Count} mappings to DB...", result.MappedCount);
                        await _context.SaveChangesAsync(cancellationToken);

                        _logger.LogInformation("Bulk re-evaluating pending orders...");

                        int updatedCount = await _context.IssOrders
                            .Where(o => o.ImportStatus == ImportStatus.NeedsMapping && o.IsActive)
                            .Where(o => !o.Lines.Any(l => _context.StockMappings.Any(m => 
                                m.ExternalStockCode == l.StockCode && 
                                (m.MatchStatus != MatchStatus.Mapped && m.MatchStatus != MatchStatus.Ignored))))
                            .ExecuteUpdateAsync(s => s.SetProperty(o => o.ImportStatus, ImportStatus.Ready), cancellationToken);

                        _logger.LogInformation("{Count} orders changed from NeedsMapping to Ready.", updatedCount);
                    }
                }

                _logger.LogInformation("--- Mapping Import End. Mapped={Mapped}, NotFoundMappings={NF1}, NotFoundStocks={NF2} ---",
                    result.MappedCount, result.NotFoundMappings.Count, result.NotFoundStocks.Count);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error during stock mapping import");
                throw;
            }

            return result;
        }
    }
}
