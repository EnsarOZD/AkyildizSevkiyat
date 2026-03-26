using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.GoodsReceipts.Commands.PostGoodsReceipt
{
    public sealed class PostGoodsReceiptCommandValidator
        : AbstractValidator<PostGoodsReceiptCommand>
    {
        public PostGoodsReceiptCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Mal kabul kimliği geçersiz.");
        }
    }
}
