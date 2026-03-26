using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.PurchaseOrders.Commands.UpdatePurchaseOrderLine
{
    public sealed class UpdatePurchaseOrderLineCommandValidator
        : AbstractValidator<UpdatePurchaseOrderLineCommand>
    {
        public UpdatePurchaseOrderLineCommandValidator()
        {
            RuleFor(x => x.PurchaseOrderId)
                .NotEmpty()
                .WithMessage("Satın alma siparişi kimliği geçersiz.");

            RuleFor(x => x.LineId)
                .NotEmpty()
                .WithMessage("Satır kimliği geçersiz.");

            RuleFor(x => x.OrderedQty)
                .GreaterThan(0)
                .WithMessage("Sipariş miktarı sıfırdan büyük olmalıdır.");
        }
    }
}
