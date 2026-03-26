using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.PurchaseOrders.Commands.CancelPurchaseOrder
{
    public sealed class CancelPurchaseOrderCommandValidator
        : AbstractValidator<CancelPurchaseOrderCommand>
    {
        public CancelPurchaseOrderCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Satın alma siparişi kimliği geçersiz.");
        }
    }
}
