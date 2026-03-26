using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.MarkReady
{
    public class MarkReadyCommandValidator : AbstractValidator<MarkReadyCommand>
    {
        public MarkReadyCommandValidator()
        {
            RuleFor(x => x.ShipmentId).GreaterThan(0);
        }
    }
}
