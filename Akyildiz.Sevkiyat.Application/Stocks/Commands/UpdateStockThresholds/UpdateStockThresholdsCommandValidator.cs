using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.Stocks.Commands.UpdateStockThresholds
{
    public sealed class UpdateStockThresholdsCommandValidator
        : AbstractValidator<UpdateStockThresholdsCommand>
    {
        public UpdateStockThresholdsCommandValidator()
        {
            RuleFor(x => x.StockMasterId)
                .GreaterThan(0)
                .WithMessage("Stok kimliği geçersiz.");

            RuleFor(x => x.MinStockQty)
                .GreaterThanOrEqualTo(0)
                .When(x => x.MinStockQty.HasValue)
                .WithMessage("Minimum stok miktarı negatif olamaz.");

            RuleFor(x => x.ReorderPoint)
                .GreaterThanOrEqualTo(0)
                .When(x => x.ReorderPoint.HasValue)
                .WithMessage("Sipariş eşiği negatif olamaz.");

            // ReorderPoint <= MinStockQty (sipariş eşiği alarm eşiğinin altında kalmalı)
            RuleFor(x => x.ReorderPoint)
                .LessThanOrEqualTo(x => x.MinStockQty!.Value)
                .When(x => x.ReorderPoint.HasValue && x.MinStockQty.HasValue)
                .WithMessage("Sipariş eşiği (ReorderPoint), minimum stok eşiğinden (MinStockQty) küçük veya eşit olmalıdır.");
        }
    }
}
