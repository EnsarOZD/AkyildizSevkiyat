using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.Stocks.Commands.CreateStock
{
    public sealed class CreateStockCommandValidator
        : AbstractValidator<CreateStockCommand>
    {
        public CreateStockCommandValidator()
        {
            RuleFor(x => x.StockCode)
                .NotEmpty()
                .WithMessage("Stok kodu boş olamaz.")
                .MaximumLength(50)
                .WithMessage("Stok kodu en fazla 50 karakter olabilir.");

            RuleFor(x => x.StockName)
                .NotEmpty()
                .WithMessage("Stok adı boş olamaz.")
                .MaximumLength(200)
                .WithMessage("Stok adı en fazla 200 karakter olabilir.");
        }
    }
}
