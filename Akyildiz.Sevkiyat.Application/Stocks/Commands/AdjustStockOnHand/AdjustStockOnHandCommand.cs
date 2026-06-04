using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Stocks.Commands.AdjustStockOnHand
{
    /// <summary>Tek ürün için harici stok sayımı / giriş modu.</summary>
    public enum StockAdjustMode
    {
        /// <summary>Girilen değer = yeni mevcut bakiye (sayım sonucu). Fark kadar düzeltme yazılır.</summary>
        Count = 0,
        /// <summary>Girilen değer mevcut bakiyenin üzerine eklenir (giriş).</summary>
        Add = 1
    }

    /// <summary>
    /// Tek bir stok kalemi için manuel sayım/giriş. Tam stok sayımından (StockCount)
    /// bağımsız — yalnızca seçilen ürünün OnHandQty'sini günceller ve ManualAdjust
    /// hareketi yazar.
    /// </summary>
    public record AdjustStockOnHandCommand : IRequest<decimal>
    {
        public int StockMasterId { get; init; }
        /// <summary>Count modunda yeni mevcut bakiye; Add modunda eklenecek miktar.</summary>
        public decimal Quantity { get; init; }
        public StockAdjustMode Mode { get; init; } = StockAdjustMode.Count;
        public string? Note { get; init; }
    }

    public class AdjustStockOnHandCommandValidator : AbstractValidator<AdjustStockOnHandCommand>
    {
        public AdjustStockOnHandCommandValidator()
        {
            RuleFor(x => x.StockMasterId).GreaterThan(0);
            RuleFor(x => x.Quantity)
                .GreaterThanOrEqualTo(0).WithMessage("Sayım sonucu negatif olamaz.")
                .When(x => x.Mode == StockAdjustMode.Count);
            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Eklenecek miktar sıfırdan büyük olmalıdır.")
                .When(x => x.Mode == StockAdjustMode.Add);
            RuleFor(x => x.Note).MaximumLength(500);
        }
    }

    public class AdjustStockOnHandCommandHandler : IRequestHandler<AdjustStockOnHandCommand, decimal>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public AdjustStockOnHandCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<decimal> Handle(AdjustStockOnHandCommand request, CancellationToken cancellationToken)
        {
            var stock = await _context.StockMasters
                .FirstOrDefaultAsync(s => s.Id == request.StockMasterId, cancellationToken)
                ?? throw new NotFoundException("StockMaster", request.StockMasterId);

            var previousQty = stock.OnHandQty;
            var diff = request.Mode == StockAdjustMode.Add
                ? request.Quantity
                : request.Quantity - previousQty;

            if (diff == 0)
                return stock.OnHandQty; // değişiklik yok

            stock.AdjustOnHand(diff); // negatif bakiye oluşursa DomainException fırlatır

            var modeLabel = request.Mode == StockAdjustMode.Add ? "Giriş" : "Sayım";
            var noteSuffix = string.IsNullOrWhiteSpace(request.Note) ? "" : $" — {request.Note}";
            _context.StockTransactions.Add(new StockTransaction
            {
                StockMasterId = stock.Id,
                Type          = StockTransactionType.ManualAdjust,
                Qty           = diff,
                Reference     = "MANUAL",
                Date          = DateTime.UtcNow,
                Note          = $"Manuel {modeLabel} ({previousQty} → {stock.OnHandQty}) · {_currentUserService.Email}{noteSuffix}"
            });

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException(
                    "Stok, aynı anda başka bir işlem tarafından güncellendi. Lütfen tekrar deneyin.");
            }

            return stock.OnHandQty;
        }
    }
}
