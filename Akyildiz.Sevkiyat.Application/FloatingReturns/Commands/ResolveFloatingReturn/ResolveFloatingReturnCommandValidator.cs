using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.FloatingReturns.Commands.ResolveFloatingReturn
{
    public class ResolveFloatingReturnCommandValidator : AbstractValidator<ResolveFloatingReturnCommand>
    {
        public ResolveFloatingReturnCommandValidator()
        {
            RuleFor(x => x.FloatingReturnId).GreaterThan(0);

            When(x => x.Action == ResolveAction.MatchToShipment, () =>
            {
                RuleFor(x => x.LinkedShipmentId)
                    .NotNull()
                    .GreaterThan(0)
                    .WithMessage("Sevkiyata eşleştirme için sevkiyat seçilmelidir.");
            });
        }
    }
}
