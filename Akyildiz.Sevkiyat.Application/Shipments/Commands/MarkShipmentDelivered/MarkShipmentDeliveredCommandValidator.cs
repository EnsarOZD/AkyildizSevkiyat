using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.MarkShipmentDelivered
{
    public class MarkShipmentDeliveredCommandValidator : AbstractValidator<MarkShipmentDeliveredCommand>
    {
        public MarkShipmentDeliveredCommandValidator()
        {
            RuleFor(x => x.ShipmentId).GreaterThan(0);
            RuleFor(x => x.DeliveryRecipient)
                .NotEmpty().WithMessage("Teslim alan kişi adı zorunludur.")
                .MaximumLength(200);
            // DeliveryNote ve DeliveryPhotoBase64 opsiyonel
        }
    }
}
