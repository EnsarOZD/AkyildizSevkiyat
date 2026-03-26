using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.GoodsReceipts.Commands.RemoveGoodsReceiptLine
{
    public sealed class RemoveGoodsReceiptLineCommandValidator
        : AbstractValidator<RemoveGoodsReceiptLineCommand>
    {
        public RemoveGoodsReceiptLineCommandValidator()
        {
            RuleFor(x => x.GoodsReceiptId)
                .NotEmpty()
                .WithMessage("Mal girişi kimliği geçersiz.");

            RuleFor(x => x.LineId)
                .NotEmpty()
                .WithMessage("Satır kimliği geçersiz.");
        }
    }
}
