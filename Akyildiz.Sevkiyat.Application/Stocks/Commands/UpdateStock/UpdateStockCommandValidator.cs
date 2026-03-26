using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.Stocks.Commands.UpdateStock
{
    public sealed class UpdateStockCommandValidator
        : AbstractValidator<UpdateStockCommand>
    {
        public UpdateStockCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Stok kimliği geçersiz.");

            RuleFor(x => x.StockName)
                .NotEmpty()
                .WithMessage("Stok adı boş olamaz.")
                .MaximumLength(200)
                .WithMessage("Stok adı en fazla 200 karakter olabilir.");

            RuleFor(x => x.MinStockQty)
                .GreaterThanOrEqualTo(0)
                .When(x => x.MinStockQty.HasValue)
                .WithMessage("Minimum stok miktarı negatif olamaz.");
        }
    }
}
