using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.Netsis.Commands.ExportShipmentToNetsis
{
    public class ExportShipmentToNetsisCommandValidator : AbstractValidator<ExportShipmentToNetsisCommand>
    {
        public ExportShipmentToNetsisCommandValidator()
        {
            RuleFor(x => x.ShipmentId).GreaterThan(0);
        }
    }
}
