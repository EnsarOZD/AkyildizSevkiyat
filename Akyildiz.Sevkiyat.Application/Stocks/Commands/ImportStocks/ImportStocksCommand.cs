using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using ExcelDataReader;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Akyildiz.Sevkiyat.Application.Stocks.Commands.ImportStocks
{
    public record ImportStocksCommand(Stream FileStream) : IRequest<ImportStocksResult>;

    public class ImportStocksResult
    {
        public int Added { get; set; }
        public int Updated { get; set; }
        public int Skipped { get; set; }
        public List<string> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public int Total => Added + Updated + Skipped;
    }

    public class ImportStocksCommandHandler : IRequestHandler<ImportStocksCommand, ImportStocksResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<ImportStocksCommandHandler> _logger;

        public ImportStocksCommandHandler(IApplicationDbContext context, ILogger<ImportStocksCommandHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ImportStocksResult> Handle(ImportStocksCommand request, CancellationToken cancellationToken)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            var result = new ImportStocksResult();
            _logger.LogInformation("--- Stock Import Start: {StartDateTime} ---", DateTime.UtcNow);

            try
            {
                using var reader = ExcelReaderFactory.CreateReader(request.FileStream);
                var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration { UseHeaderRow = true }
                });

                if (dataSet.Tables.Count == 0)
                {
                    result.Errors.Add("Excel dosyasında sayfa bulunamadı.");
                    return result;
                }

                var table = dataSet.Tables[0];
                int rowIndex = 0;

                foreach (DataRow row in table.Rows)
                {
                    rowIndex++;
                    try
                    {
                        var code = GetVal(row, 0);
                        if (string.IsNullOrWhiteSpace(code))
                        {
                            result.Skipped++;
                            continue;
                        }

                        var name = GetVal(row, 1);
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            result.Warnings.Add($"Satır {rowIndex}: Stok kodu '{code}' için ad boş — satır atlandı.");
                            result.Skipped++;
                            continue;
                        }

                        var unitStr      = GetVal(row, 2);
                        var priceStr     = GetVal(row, 3)?.Replace(",", ".");
                        var taxStr       = GetVal(row, 4)?.Replace(",", ".");
                        var pickingStr   = GetVal(row, 5);
                        var statusStr    = GetVal(row, 6);
                        var categoryStr  = GetVal(row, 7);

                        // Parse Price
                        decimal unitPrice = 0;
                        if (decimal.TryParse(priceStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal p))
                            unitPrice = p;

                        // Parse Tax
                        var taxRate = Akyildiz.Sevkiyat.Domain.Enums.TaxRate.Percent20;
                        if (decimal.TryParse(taxStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal t))
                        {
                            taxRate = (int)t switch
                            {
                                0  => Akyildiz.Sevkiyat.Domain.Enums.TaxRate.Percent0,
                                1  => Akyildiz.Sevkiyat.Domain.Enums.TaxRate.Percent1,
                                10 => Akyildiz.Sevkiyat.Domain.Enums.TaxRate.Percent10,
                                20 => Akyildiz.Sevkiyat.Domain.Enums.TaxRate.Percent20,
                                _  => Akyildiz.Sevkiyat.Domain.Enums.TaxRate.Percent20
                            };
                        }

                        // Parse Picking Type
                        var pickingType = Akyildiz.Sevkiyat.Domain.Enums.PickingType.Unassigned;
                        if (!string.IsNullOrWhiteSpace(pickingStr))
                        {
                            if (pickingStr.Contains("Micro", StringComparison.OrdinalIgnoreCase))
                                pickingType = Akyildiz.Sevkiyat.Domain.Enums.PickingType.Micro;
                            else if (pickingStr.Contains("Macro", StringComparison.OrdinalIgnoreCase))
                                pickingType = Akyildiz.Sevkiyat.Domain.Enums.PickingType.Macro;
                        }

                        if (pickingType == Akyildiz.Sevkiyat.Domain.Enums.PickingType.Unassigned)
                            result.Warnings.Add($"Satır {rowIndex}: '{code}' — Toplama tipi belirtilmemiş (Micro/Macro). Unassigned olarak kaydedildi.");

                        // Parse Status
                        bool isActive = true;
                        if (!string.IsNullOrWhiteSpace(statusStr))
                            isActive = !statusStr.Contains("Pasif", StringComparison.OrdinalIgnoreCase) && statusStr != "0";

                        // Parse Category
                        var category = Akyildiz.Sevkiyat.Domain.Enums.StockCategory.Tanimsiz;
                        if (!string.IsNullOrWhiteSpace(categoryStr))
                        {
                            if (!Enum.TryParse<Akyildiz.Sevkiyat.Domain.Enums.StockCategory>(categoryStr, true, out category))
                            {
                                category = categoryStr.ToUpper().Replace("İ", "I") switch
                                {
                                    "GIDA" or "YEMEK"                   => Akyildiz.Sevkiyat.Domain.Enums.StockCategory.Gida,
                                    "SARF" or "TEMIZLIK" or "HIJYEN"    => Akyildiz.Sevkiyat.Domain.Enums.StockCategory.Sarf,
                                    "KIYAFET" or "GIYIM" or "ELBISE"    => Akyildiz.Sevkiyat.Domain.Enums.StockCategory.Kiyafet,
                                    "KIRTASIYE" or "OFIS"               => Akyildiz.Sevkiyat.Domain.Enums.StockCategory.Kirtasiye,
                                    _                                    => Akyildiz.Sevkiyat.Domain.Enums.StockCategory.Diger
                                };
                            }
                        }

                        // Parse Unit
                        var unit = Akyildiz.Sevkiyat.Domain.Enums.StockUnit.Adet;
                        if (!string.IsNullOrWhiteSpace(unitStr))
                        {
                            if (!Enum.TryParse<Akyildiz.Sevkiyat.Domain.Enums.StockUnit>(unitStr, true, out unit))
                            {
                                unit = unitStr.ToLower()
                                    .Replace("ı", "i").Replace("ş", "s").Replace("ç", "c")
                                    .Replace("ğ", "g").Replace("ü", "u").Replace("ö", "o") switch
                                {
                                    "adet" or "ad" or "ad."      => Akyildiz.Sevkiyat.Domain.Enums.StockUnit.Adet,
                                    "kg" or "kilogram" or "kilo" => Akyildiz.Sevkiyat.Domain.Enums.StockUnit.Kg,
                                    "paket" or "pk" or "pk."     => Akyildiz.Sevkiyat.Domain.Enums.StockUnit.Paket,
                                    "koli" or "kl" or "kl."      => Akyildiz.Sevkiyat.Domain.Enums.StockUnit.Koli,
                                    "litre" or "lt" or "lt."     => Akyildiz.Sevkiyat.Domain.Enums.StockUnit.Litre,
                                    "metre" or "mt" or "mt."     => Akyildiz.Sevkiyat.Domain.Enums.StockUnit.Metre,
                                    "metrekare" or "m2"          => Akyildiz.Sevkiyat.Domain.Enums.StockUnit.Metrekare,
                                    "set"                        => Akyildiz.Sevkiyat.Domain.Enums.StockUnit.Set,
                                    "teneke" or "tnk"            => Akyildiz.Sevkiyat.Domain.Enums.StockUnit.Teneke,
                                    _                            => Akyildiz.Sevkiyat.Domain.Enums.StockUnit.Diger
                                };
                            }
                        }

                        var existing = await _context.StockMasters
                            .FirstOrDefaultAsync(s => s.StockCode == code, cancellationToken);

                        if (existing == null)
                        {
                            _context.StockMasters.Add(new StockMaster
                            {
                                StockCode   = code,
                                StockName   = name,
                                Unit        = unit,
                                UnitPrice   = unitPrice,
                                TaxRate     = taxRate,
                                PickingType = pickingType,
                                Category    = category,
                                IsActive    = isActive
                            });
                            result.Added++;
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(name))  existing.StockName = name;
                            existing.Unit      = unit;
                            existing.UnitPrice = unitPrice;
                            existing.TaxRate   = taxRate;
                            if (pickingType != Akyildiz.Sevkiyat.Domain.Enums.PickingType.Unassigned)
                                existing.PickingType = pickingType;
                            existing.IsActive  = isActive;
                            existing.Category  = category;
                            result.Updated++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error on Row {RowIndex}", rowIndex);
                        result.Errors.Add($"Satır {rowIndex}: {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("--- Stock Import End. Added={Added}, Updated={Updated}, Skipped={Skipped}, Warnings={Warnings}, Errors={Errors} ---",
                    result.Added, result.Updated, result.Skipped, result.Warnings.Count, result.Errors.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Global Import Error");
                result.Errors.Add($"Dosya okuma hatası: {ex.Message}");
            }

            return result;
        }

        private static string? GetVal(DataRow row, int index)
        {
            if (index >= row.Table.Columns.Count) return null;
            return row[index]?.ToString()?.Trim();
        }
    }
}
