using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.ConfirmZoneLoading
{
    public sealed class ConfirmZoneLoadingCommandValidator
        : AbstractValidator<ConfirmZoneLoadingCommand>
    {
        public ConfirmZoneLoadingCommandValidator()
        {
            RuleFor(x => x.ZonePreparationId)
                .GreaterThan(0)
                .WithMessage("Bölge hazırlık kimliği geçersiz.");
        }
    }
}
