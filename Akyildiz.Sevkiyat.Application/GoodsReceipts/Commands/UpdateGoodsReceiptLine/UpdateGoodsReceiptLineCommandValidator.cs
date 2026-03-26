using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.GoodsReceipts.Commands.UpdateGoodsReceiptLine
{
    public sealed class UpdateGoodsReceiptLineCommandValidator
        : AbstractValidator<UpdateGoodsReceiptLineCommand>
    {
        public UpdateGoodsReceiptLineCommandValidator()
        {
            RuleFor(x => x.GoodsReceiptId)
                .NotEmpty()
                .WithMessage("Mal kabul kimliği geçersiz.");

            RuleFor(x => x.LineId)
                .NotEmpty()
                .WithMessage("Satır kimliği geçersiz.");

            RuleFor(x => x.ReceivedQty)
                .GreaterThan(0)
                .WithMessage("Teslim alınan miktar 0'dan büyük olmalıdır.");

            RuleFor(x => x.RejectedQty)
                .GreaterThanOrEqualTo(0)
                .When(x => x.RejectedQty.HasValue)
                .WithMessage("Reddedilen miktar negatif olamaz.");

            RuleFor(x => x.RejectReason)
                .NotEmpty()
                .When(x => x.RejectedQty.HasValue && x.RejectedQty.Value > 0)
                .WithMessage("Reddedilen miktar girildiğinde red nedeni zorunludur.");
        }
    }
}
