using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.StartPicking
{
    public class StartPickingCommandValidator : AbstractValidator<StartPickingCommand>
    {
        public StartPickingCommandValidator()
        {
            RuleFor(x => x.ShipmentId).GreaterThan(0);
        }
    }
}
