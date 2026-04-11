using Akyildiz.Sevkiyat.Domain.Enums;
using FluentValidation;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.UpdateShipmentDetails
{
    public record ShipmentLineUpdateDto(int? LineId, string StockCode, string StockName, decimal OrderedQty, StockUnit Unit);
    public record UpdateShipmentDetailsCommand(int ShipmentId, DateTime DeliveryDate, List<ShipmentLineUpdateDto> Lines) : IRequest<Unit>;

    public class UpdateShipmentDetailsCommandValidator : AbstractValidator<UpdateShipmentDetailsCommand>
    {
        private static readonly StockUnit[] ValidUnits = (StockUnit[])Enum.GetValues(typeof(StockUnit));

        public UpdateShipmentDetailsCommandValidator()
        {
            RuleFor(x => x.ShipmentId)
                .GreaterThan(0).WithMessage("Geçerli bir sevkiyat ID'si girilmelidir.");

            RuleFor(x => x.DeliveryDate)
                .NotEmpty().WithMessage("Teslimat tarihi boş olamaz.")
                .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
                    .WithMessage("Teslimat tarihi bugün veya sonrası olmalıdır.");

            RuleFor(x => x.Lines)
                .NotNull().WithMessage("Satır listesi boş olamaz.")
                .Must(l => l.Count > 0).WithMessage("En az bir satır girilmelidir.");

            RuleForEach(x => x.Lines).ChildRules(line =>
            {
                line.RuleFor(l => l.LineId)
                    .GreaterThan(0).When(l => l.LineId.HasValue)
                    .WithMessage("Satır ID'si 0'dan büyük olmalıdır.");

                line.RuleFor(l => l.StockCode)
                    .NotEmpty().WithMessage("Stok kodu boş olamaz.")
                    .MaximumLength(50).WithMessage("Stok kodu en fazla 50 karakter olabilir.");

                line.RuleFor(l => l.StockName)
                    .NotEmpty().WithMessage("Stok adı boş olamaz.")
                    .MaximumLength(200).WithMessage("Stok adı en fazla 200 karakter olabilir.");

                line.RuleFor(l => l.OrderedQty)
                    .GreaterThan(0).WithMessage("Sipariş miktarı 0'dan büyük olmalıdır.");

                line.RuleFor(l => l.Unit)
                    .Must(u => ValidUnits.Contains(u))
                    .WithMessage("Geçersiz birim değeri.");
            });
        }
    }
}
