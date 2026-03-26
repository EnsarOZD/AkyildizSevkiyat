using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.StockCounts.Commands.ImportStockCountFromExcel
{
    public class ImportStockCountFromExcelCommand : IRequest<ImportStockCountResult>
    {
        public int StockCountId { get; set; }
        public byte[] FileContent { get; set; } = null!;
    }

    public class ImportStockCountResult
    {
        public int UpdatedCount { get; set; }
        public int SkippedCount { get; set; }
        public int ErrorCount { get; set; }
    }

    public class ImportStockCountFromExcelCommandHandler 
        : IRequestHandler<ImportStockCountFromExcelCommand, ImportStockCountResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly IStockCountExcelService _excelService;

        public ImportStockCountFromExcelCommandHandler(
            IApplicationDbContext context, 
            IStockCountExcelService excelService)
        {
            _context = context;
            _excelService = excelService;
        }

        public async Task<ImportStockCountResult> Handle(ImportStockCountFromExcelCommand request, CancellationToken cancellationToken)
        {
            var stockCount = await _context.StockCounts
                .Include(sc => sc.Lines)
                .FirstOrDefaultAsync(sc => sc.Id == request.StockCountId, cancellationToken);

            if (stockCount == null)
                throw new NotFoundException("Sayım kaydı bulunamadı.");

            if (stockCount.Status != StockCountStatus.Draft)
                throw new DomainException("Sadece taslak/devam eden sayımlar için Excel yüklenebilir.");

            var parsedRows = _excelService.ParseImportData(request.FileContent);

            var dbLinesDict = stockCount.Lines.ToDictionary(l => l.Id);

            int updated = 0;
            int skipped = 0;

            foreach (var row in parsedRows)
            {
                if (!row.LineId.HasValue)
                    continue;

                if (!dbLinesDict.TryGetValue(row.LineId.Value, out var dbLine))
                    throw new DomainException($"Satır {row.RowNumber}: Excel'deki LineId ({row.LineId}) bu sayım kaydına ait değil veya veritabanında bulunamadı.");

                if (!row.ActualQty.HasValue)
                {
                    skipped++;
                    continue;
                }

                if (dbLine.ActualQty != row.ActualQty)
                {
                    dbLine.ActualQty = row.ActualQty;
                    updated++;
                }
                else
                {
                    skipped++;
                }
            }

            if (updated > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            return new ImportStockCountResult
            {
                UpdatedCount = updated,
                SkippedCount = skipped,
                ErrorCount = 0
            };
        }
    }
}
