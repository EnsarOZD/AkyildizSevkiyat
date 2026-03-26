using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.GoodsReceipts.Commands.CancelGoodsReceipt
{
    public sealed class CancelGoodsReceiptCommandValidator
        : AbstractValidator<CancelGoodsReceiptCommand>
    {
        public CancelGoodsReceiptCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Mal kabul kimliği geçersiz.");
        }
    }
}
