using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.AssignVehicle
{
    public class AssignVehicleCommandValidator : AbstractValidator<AssignVehicleCommand>
    {
        public AssignVehicleCommandValidator()
        {
            RuleFor(x => x.ShipmentId).GreaterThan(0);
            RuleFor(x => x.DriverId).GreaterThan(0).WithMessage("Şoför seçimi zorunludur.");
            RuleFor(x => x.VehicleId).GreaterThan(0).WithMessage("Araç seçimi zorunludur.");
        }
    }
}
