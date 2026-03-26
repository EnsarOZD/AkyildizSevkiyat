using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.PurchaseOrders.Commands.ClosePurchaseOrder
{
    public sealed class ClosePurchaseOrderCommandValidator
        : AbstractValidator<ClosePurchaseOrderCommand>
    {
        public ClosePurchaseOrderCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Satın alma siparişi kimliği geçersiz.");
        }
    }
}
