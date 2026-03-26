using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.PurchaseOrders.Commands.RemovePurchaseOrderLine
{
    public sealed class RemovePurchaseOrderLineCommandValidator
        : AbstractValidator<RemovePurchaseOrderLineCommand>
    {
        public RemovePurchaseOrderLineCommandValidator()
        {
            RuleFor(x => x.PurchaseOrderId)
                .NotEmpty()
                .WithMessage("Satın alma siparişi kimliği geçersiz.");

            RuleFor(x => x.LineId)
                .NotEmpty()
                .WithMessage("Satır kimliği geçersiz.");
        }
    }
}
