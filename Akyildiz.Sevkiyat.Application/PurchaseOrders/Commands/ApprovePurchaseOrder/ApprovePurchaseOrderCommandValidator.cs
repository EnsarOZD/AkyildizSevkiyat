using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.PurchaseOrders.Commands.ApprovePurchaseOrder
{
    public sealed class ApprovePurchaseOrderCommandValidator
        : AbstractValidator<ApprovePurchaseOrderCommand>
    {
        public ApprovePurchaseOrderCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Satın alma siparişi kimliği geçersiz.");
        }
    }
}
