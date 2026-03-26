using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.GoodsReceipts.Commands.CreateCorrectionGoodsReceipt
{
    public sealed class CreateCorrectionGoodsReceiptCommandValidator
        : AbstractValidator<CreateCorrectionGoodsReceiptCommand>
    {
        public CreateCorrectionGoodsReceiptCommandValidator()
        {
            RuleFor(x => x.OriginalGoodsReceiptId)
                .NotEmpty()
                .WithMessage("Orijinal mal girişi kimliği geçersiz.");
        }
    }
}
