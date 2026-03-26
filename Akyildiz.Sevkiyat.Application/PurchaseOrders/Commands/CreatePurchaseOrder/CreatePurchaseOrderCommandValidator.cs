using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.PurchaseOrders.Commands.CreatePurchaseOrder
{
    public sealed class CreatePurchaseOrderCommandValidator
        : AbstractValidator<CreatePurchaseOrderCommand>
    {
        public CreatePurchaseOrderCommandValidator()
        {
            RuleFor(x => x.SupplierId)
                .NotEmpty()
                .WithMessage("Tedarikçi seçilmelidir.");

            RuleFor(x => x.OrderDate)
                .NotEmpty()
                .WithMessage("Sipariş tarihi boş olamaz.");

            RuleFor(x => x.Lines)
                .NotEmpty()
                .WithMessage("En az bir satır girilmelidir.");

            RuleForEach(x => x.Lines).ChildRules(line =>
            {
                line.RuleFor(l => l.StockMasterId)
                    .GreaterThan(0)
                    .WithMessage("Stok seçilmelidir.");

                line.RuleFor(l => l.OrderedQty)
                    .GreaterThan(0)
                    .WithMessage("Sipariş miktarı 0'dan büyük olmalıdır.");
            });
        }
    }
}
