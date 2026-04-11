using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.GoodsReceipts.Commands.CreateGoodsReceipt
{
    public sealed class CreateGoodsReceiptCommandValidator
        : AbstractValidator<CreateGoodsReceiptCommand>
    {
        public CreateGoodsReceiptCommandValidator()
        {
            RuleFor(x => x.WaybillNo)
                .NotEmpty()
                .WithMessage("İrsaliye numarası boş olamaz.");

            RuleFor(x => x.WaybillDate)
                .NotEmpty()
                .WithMessage("İrsaliye tarihi boş olamaz.");

            // Either a single PO, multiple POs, or at least a SupplierId must be present
            RuleFor(x => x)
                .Must(x => x.PurchaseOrderId.HasValue || (x.PurchaseOrderIds != null && x.PurchaseOrderIds.Any()) || x.SupplierId.HasValue)
                .WithMessage("Sipariş veya tedarikçi seçimi yapılmalıdır.");

            // Mandatory note for PO-less entries
            RuleFor(x => x.Note)
                .NotEmpty()
                .When(x => !x.PurchaseOrderId.HasValue && (x.PurchaseOrderIds == null || !x.PurchaseOrderIds.Any()))
                .WithMessage("Siparişsiz mal kabulü için açıklama (not) girmek zorunludur.");
        }
    }
}
