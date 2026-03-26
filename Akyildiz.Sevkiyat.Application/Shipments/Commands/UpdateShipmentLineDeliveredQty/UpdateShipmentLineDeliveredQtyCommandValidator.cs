using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.UpdateShipmentLineDeliveredQty
{
    public sealed class UpdateShipmentLineDeliveredQtyCommandValidator
        : AbstractValidator<UpdateShipmentLineDeliveredQtyCommand>
    {
        public UpdateShipmentLineDeliveredQtyCommandValidator()
        {
            RuleFor(x => x.ShipmentId).GreaterThan(0);
            RuleFor(x => x.LineId).GreaterThan(0);
            RuleFor(x => x.DeliveredQty).GreaterThanOrEqualTo(0);

            When(x => x.DeliveredQty != 0, () =>
            {
                // Not: asıl “DeliveredQty != OrderedQty ise reason zorunlu”
                // domain’de. Validation burada sadece boş gelmesin diye yardımcı.
                RuleFor(x => x.Note).MaximumLength(500);
                RuleFor(x => x.DifferenceReason).MaximumLength(100);
            });
        }
    }
}
