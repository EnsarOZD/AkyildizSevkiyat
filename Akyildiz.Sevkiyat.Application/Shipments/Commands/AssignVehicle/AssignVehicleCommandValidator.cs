using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.AssignVehicle
{
    public class AssignVehicleCommandValidator : AbstractValidator<AssignVehicleCommand>
    {
        public AssignVehicleCommandValidator()
        {
            RuleFor(x => x.ShipmentId).GreaterThan(0);
            RuleFor(x => x.DriverName).NotEmpty().WithMessage("Sürücü ismi gereklidir.");
            RuleFor(x => x.PlateNumber).NotEmpty().WithMessage("Plaka gereklidir.");
        }
    }
}
