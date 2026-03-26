using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using ExcelDataReader;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Data;
using System.IO;
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

                foreach (DataRow row in table.Rows)
                {
                    var externalCode = row[0]?.ToString()?.Trim();
                    var localCode    = row[2]?.ToString()?.Trim();

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

                    _logger.LogInformation("Processing: {ExternalCode} -> {LocalCode}", externalCode, localCode);

                    // Find the mapping entry
                    var mapping = await _context.StockMappings
                        .FirstOrDefaultAsync(m => m.ExternalStockCode == externalCode, cancellationToken);

                    if (mapping == null)
                    {
                        _logger.LogWarning("Mapping not found for external code: {ExternalCode}", externalCode);
                        result.NotFoundMappings.Add(externalCode);
                        continue;
                    }

                    // Find the local stock
                    var localStockId = await _context.StockMasters
                        .Where(s => s.StockCode == localCode)
                        .Select(s => (int?)s.Id)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (localStockId == null)
                    {
                        _logger.LogWarning("Local stock not found for code: {LocalStockCode}", localCode);
                        result.NotFoundStocks.Add($"'{localCode}' (ISS: {externalCode} için)");
                        continue;
                    }

                    mapping.LocalStockId = localStockId;
                    mapping.MatchStatus  = MatchStatus.Mapped;
                    result.MappedCount++;
                }

                if (result.MappedCount > 0)
                {
                    _logger.LogInformation("Saving {Count} mappings...", result.MappedCount);
                    await _context.SaveChangesAsync(cancellationToken);

                    // Re-evaluate NeedsMapping orders
                    var pendingOrderIds = await _context.IssOrders
                        .Where(o => o.ImportStatus == ImportStatus.NeedsMapping && o.IsActive)
                        .Select(o => o.Id)
                        .ToListAsync(cancellationToken);

                    _logger.LogInformation("Checking {Count} pending orders...", pendingOrderIds.Count);

                    foreach (var orderId in pendingOrderIds)
                    {
                        var stockCodes = await _context.IssOrderLines
                            .Where(l => l.IssOrderId == orderId)
                            .Select(l => l.StockCode)
                            .ToListAsync(cancellationToken);

                        bool allMapped = true;
                        foreach (var stockCode in stockCodes)
                        {
                            var isMapped = await _context.StockMappings
                                .AnyAsync(m => m.ExternalStockCode == stockCode &&
                                               (m.MatchStatus == MatchStatus.Mapped || m.MatchStatus == MatchStatus.Ignored),
                                          cancellationToken);
                            if (!isMapped) { allMapped = false; break; }
                        }

                        if (allMapped)
                        {
                            await _context.IssOrders
                                .Where(o => o.Id == orderId)
                                .ExecuteUpdateAsync(s => s.SetProperty(o => o.ImportStatus, ImportStatus.Ready), cancellationToken);
                        }
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
