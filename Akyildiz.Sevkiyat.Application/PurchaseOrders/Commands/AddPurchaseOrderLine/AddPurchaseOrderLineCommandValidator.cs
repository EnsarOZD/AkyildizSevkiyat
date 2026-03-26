using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.PurchaseOrders.Commands.AddPurchaseOrderLine
{
    public sealed class AddPurchaseOrderLineCommandValidator
        : AbstractValidator<AddPurchaseOrderLineCommand>
    {
        public AddPurchaseOrderLineCommandValidator()
        {
            RuleFor(x => x.PurchaseOrderId)
                .NotEmpty()
                .WithMessage("Satın alma siparişi kimliği geçersiz.");

            RuleFor(x => x.StockMasterId)
                .GreaterThan(0)
                .WithMessage("Stok seçilmelidir.");

            RuleFor(x => x.OrderedQty)
                .GreaterThan(0)
                .WithMessage("Sipariş miktarı 0'dan büyük olmalıdır.");
        }
    }
}
