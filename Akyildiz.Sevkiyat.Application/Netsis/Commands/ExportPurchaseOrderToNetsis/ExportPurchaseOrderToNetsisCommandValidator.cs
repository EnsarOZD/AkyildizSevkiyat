using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.Netsis.Commands.ExportPurchaseOrderToNetsis
{
    public class ExportPurchaseOrderToNetsisCommandValidator : AbstractValidator<ExportPurchaseOrderToNetsisCommand>
    {
        public ExportPurchaseOrderToNetsisCommandValidator()
        {
            RuleFor(x => x.PurchaseOrderId)
                .NotEmpty().WithMessage("PurchaseOrderId zorunludur.");
        }
    }
}
