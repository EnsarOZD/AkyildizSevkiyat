using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.CreateShipment
{
    public sealed class CreateShipmentCommandValidator
        : AbstractValidator<CreateShipmentCommand>
    {
        public CreateShipmentCommandValidator()
        {
            RuleFor(x => x.IssOrderId)
                .GreaterThan(0)
                .WithMessage("IssOrderId 0'dan büyük olmalıdır.");
        }
    }
}
