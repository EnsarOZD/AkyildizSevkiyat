using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.RecordVehicleReturn
{
    public class RecordVehicleReturnCommandValidator : AbstractValidator<RecordVehicleReturnCommand>
    {
        public RecordVehicleReturnCommandValidator()
        {
            RuleFor(x => x.ShipmentId).GreaterThan(0);

            RuleFor(x => x.Lines)
                .NotEmpty().WithMessage("En az bir iade satırı girilmelidir.");

            RuleForEach(x => x.Lines).ChildRules(line =>
            {
                line.RuleFor(l => l.ShipmentLineId).GreaterThan(0);
                line.RuleFor(l => l.ReturnedQty).GreaterThan(0).WithMessage("İade miktarı sıfırdan büyük olmalıdır.");
            });
        }
    }
}
