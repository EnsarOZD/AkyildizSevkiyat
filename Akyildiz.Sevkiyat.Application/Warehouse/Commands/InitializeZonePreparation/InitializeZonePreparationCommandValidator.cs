using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.InitializeZonePreparation
{
    public sealed class InitializeZonePreparationCommandValidator
        : AbstractValidator<InitializeZonePreparationCommand>
    {
        public InitializeZonePreparationCommandValidator()
        {
            RuleFor(x => x.ZoneId)
                .GreaterThan(0)
                .WithMessage("Bölge kimliği geçersiz.");

            RuleFor(x => x.DeliveryDate)
                .NotEmpty()
                .WithMessage("Teslimat tarihi boş olamaz.");
        }
    }
}
