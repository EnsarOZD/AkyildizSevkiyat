using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.CreateManualShipment
{
    public sealed class CreateManualShipmentCommandValidator
        : AbstractValidator<CreateManualShipmentCommand>
    {
        public CreateManualShipmentCommandValidator()
        {
            RuleFor(x => x.CustomerId).GreaterThan(0);

            RuleFor(x => x.DeliveryDate)
                .NotEmpty()
                .WithMessage("Teslim tarihi zorunludur.");

            RuleFor(x => x.Lines)
                .NotEmpty()
                .WithMessage("En az bir kalem eklenmelidir.");

            RuleForEach(x => x.Lines).ChildRules(line =>
            {
                line.RuleFor(l => l.StockMasterId).GreaterThan(0);
                line.RuleFor(l => l.Qty)
                    .GreaterThan(0)
                    .WithMessage("Kalem miktarı sıfırdan büyük olmalıdır.");
            });
        }
    }
}
