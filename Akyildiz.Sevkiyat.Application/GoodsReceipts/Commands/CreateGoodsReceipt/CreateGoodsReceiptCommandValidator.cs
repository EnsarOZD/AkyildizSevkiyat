using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.GoodsReceipts.Commands.CreateGoodsReceipt
{
    public sealed class CreateGoodsReceiptCommandValidator
        : AbstractValidator<CreateGoodsReceiptCommand>
    {
        public CreateGoodsReceiptCommandValidator()
        {
            RuleFor(x => x.PurchaseOrderId)
                .NotEmpty()
                .WithMessage("Satın alma siparişi seçilmelidir.");

            RuleFor(x => x.WaybillNo)
                .NotEmpty()
                .WithMessage("İrsaliye numarası boş olamaz.");

            RuleFor(x => x.WaybillDate)
                .NotEmpty()
                .WithMessage("İrsaliye tarihi boş olamaz.");
        }
    }
}
