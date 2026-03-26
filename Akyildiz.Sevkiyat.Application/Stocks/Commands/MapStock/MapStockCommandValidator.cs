using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.Stocks.Commands.MapStock
{
    public sealed class MapStockCommandValidator
        : AbstractValidator<MapStockCommand>
    {
        public MapStockCommandValidator()
        {
            RuleFor(x => x.MappingId)
                .GreaterThan(0)
                .WithMessage("Eşleme kimliği geçersiz.");
        }
    }
}
