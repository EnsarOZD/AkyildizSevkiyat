using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.AssignToWarehouse
{
    public class AssignToWarehouseCommandValidator : AbstractValidator<AssignToWarehouseCommand>
    {
        public AssignToWarehouseCommandValidator()
        {
            RuleFor(x => x.ShipmentId).GreaterThan(0).WithMessage("Geçerli bir ShipmentId giriniz.");
        }
    }
}
