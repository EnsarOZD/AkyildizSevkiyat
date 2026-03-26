using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.MarkProjectMicroReady
{
    public sealed class MarkProjectMicroReadyCommandValidator
        : AbstractValidator<MarkProjectMicroReadyCommand>
    {
        public MarkProjectMicroReadyCommandValidator()
        {
            RuleFor(x => x.ZonePreparationProjectId)
                .GreaterThan(0)
                .WithMessage("Proje hazırlık kimliği geçersiz.");

            RuleFor(x => x.ForceReason)
                .NotEmpty()
                .When(x => x.ForceComplete)
                .WithMessage("Zorunlu tamamlama kullanılırken neden girilmesi zorunludur.");
        }
    }
}
