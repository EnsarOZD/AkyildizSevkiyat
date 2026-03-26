using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.StartZonePreparation
{
    public sealed class StartZonePreparationCommandValidator
        : AbstractValidator<StartZonePreparationCommand>
    {
        public StartZonePreparationCommandValidator()
        {
            RuleFor(x => x.ZonePreparationId)
                .GreaterThan(0)
                .WithMessage("Bölge hazırlık kimliği geçersiz.");
        }
    }
}
