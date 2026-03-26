using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.PurchaseOrders.Commands.UpdatePurchaseOrder
{
    public sealed class UpdatePurchaseOrderCommandValidator
        : AbstractValidator<UpdatePurchaseOrderCommand>
    {
        public UpdatePurchaseOrderCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Satın alma siparişi kimliği geçersiz.");

            RuleFor(x => x.OrderDate)
                .NotEmpty()
                .WithMessage("Sipariş tarihi zorunludur.");

            RuleFor(x => x.ExpectedDeliveryDate)
                .GreaterThanOrEqualTo(x => x.OrderDate)
                .When(x => x.ExpectedDeliveryDate.HasValue)
                .WithMessage("Beklenen teslim tarihi sipariş tarihinden önce olamaz.");
        }
    }
}
