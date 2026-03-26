using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.MarkZoneMacroReady
{
    public sealed class MarkZoneMacroReadyCommandValidator
        : AbstractValidator<MarkZoneMacroReadyCommand>
    {
        public MarkZoneMacroReadyCommandValidator()
        {
            RuleFor(x => x.ZonePreparationId)
                .GreaterThan(0)
                .WithMessage("Bölge hazırlık kimliği geçersiz.");

            RuleFor(x => x.ForceReason)
                .NotEmpty()
                .When(x => x.ForceComplete)
                .WithMessage("Zorunlu tamamlama kullanılırken neden girilmesi zorunludur.");
        }
    }
}
