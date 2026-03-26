using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.StockCounts.Commands.CompleteStockCount
{
    /// <summary>
    /// Sayımı tamamlar:
    ///  - ActualQty girilmiş her satır için fark hesaplanır
    ///  - Fark ≠ 0 ise ManualAdjust StockTransaction oluşturulur ve OnHandQty güncellenir
    ///  - Sayım Completed durumuna alınır
    /// </summary>
    public record CompleteStockCountCommand(int StockCountId) : IRequest<CompleteStockCountResult>;

    public record CompleteStockCountResult(
        int AdjustedLines,
        decimal TotalPositiveDiff,
        decimal TotalNegativeDiff
    );

    public class CompleteStockCountCommandHandler : IRequestHandler<CompleteStockCountCommand, CompleteStockCountResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CompleteStockCountCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<CompleteStockCountResult> Handle(CompleteStockCountCommand request, CancellationToken cancellationToken)
        {
            var stockCount = await _context.StockCounts
                .Include(c => c.Lines)
                .FirstOrDefaultAsync(c => c.Id == request.StockCountId, cancellationToken);

            if (stockCount == null)
                throw new NotFoundException("StockCount", request.StockCountId);

            if (stockCount.Status != StockCountStatus.Draft)
                throw new DomainException("Bu sayım zaten tamamlanmış.");

            // Only process lines where ActualQty was entered
            var countedLines = stockCount.Lines.Where(l => l.ActualQty.HasValue).ToList();

            if (countedLines.Count == 0)
                throw new DomainException("Hiçbir satır sayılmamış. En az bir satır girilmelidir.");

            // Load stock masters for counted lines
            var stockMasterIds = countedLines.Select(l => l.StockMasterId).Distinct().ToList();
            var stocks = await _context.StockMasters
                .Where(s => stockMasterIds.Contains(s.Id))
                .ToListAsync(cancellationToken);
            var stockMap = stocks.ToDictionary(s => s.Id);

            int adjustedLines = 0;
            decimal totalPositive = 0;
            decimal totalNegative = 0;

            foreach (var line in countedLines)
            {
                var diff = line.ActualQty!.Value - line.ExpectedQty;
                if (diff == 0) continue;

                if (!stockMap.TryGetValue(line.StockMasterId, out var stock)) continue;

                stock.AdjustOnHand(diff);

                _context.StockTransactions.Add(new StockTransaction
                {
                    StockMasterId = stock.Id,
                    Type = StockTransactionType.ManualAdjust,
                    Qty = diff,
                    Reference = $"CNT-{stockCount.Id}",
                    Date = DateTime.UtcNow,
                    Note = $"Stok sayımı #{stockCount.Id} düzeltme fişi{(line.Note != null ? ": " + line.Note : "")}"
                });

                adjustedLines++;
                if (diff > 0) totalPositive += diff;
                else totalNegative += diff;
            }

            stockCount.Status = StockCountStatus.Completed;
            stockCount.CompletedAt = DateTime.UtcNow;
            stockCount.CompletedByUserId = _currentUserService.UserId;

            await _context.SaveChangesAsync(cancellationToken);

            return new CompleteStockCountResult(adjustedLines, totalPositive, totalNegative);
        }
    }
}
