using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.Stocks.Commands.DeleteStock
{
    public sealed class DeleteStockCommandValidator
        : AbstractValidator<DeleteStockCommand>
    {
        public DeleteStockCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Stok kimliği geçersiz.");
        }
    }
}
