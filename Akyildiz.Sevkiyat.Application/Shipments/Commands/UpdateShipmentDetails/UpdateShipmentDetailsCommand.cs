using Akyildiz.Sevkiyat.Domain.Enums;
using FluentValidation;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.UpdateShipmentDetails
{
    /// <summary>Termin (teslim) tarihi değiştiğinde girilen sebep. Postpone → projeye erteleme maili.</summary>
    public enum DeliveryDateChangeReason
    {
        None = 0,       // tarih değişmedi
        Postpone = 1,   // Erteleme — projeye bildirim maili gönderilir
        Other = 2       // Diğer — mail gönderilmez
    }

    public record UpdateShipmentDetailsResult(bool DateChanged, bool EmailSent, string? EmailError);

    public class ShipmentLineUpdateDto
    {
        public int? LineId { get; set; }
        public string StockCode { get; set; } = string.Empty;
        public string StockName { get; set; } = string.Empty;
        public decimal OrderedQty { get; set; }
        public StockUnit Unit { get; set; }

        public ShipmentLineUpdateDto() { }

        public ShipmentLineUpdateDto(int? lineId, string stockCode, string stockName, decimal orderedQty, StockUnit unit)
        {
            LineId = lineId;
            StockCode = stockCode;
            StockName = stockName;
            OrderedQty = orderedQty;
            Unit = unit;
        }
    }

    public class UpdateShipmentDetailsCommand : IRequest<UpdateShipmentDetailsResult>
    {
        public int ShipmentId { get; set; }
        public DateTime DeliveryDate { get; set; }
        public List<ShipmentLineUpdateDto> Lines { get; set; } = new();

        /// <summary>Termin tarihi değiştiyse sebep. Postpone → erteleme maili gönderilir.</summary>
        public DeliveryDateChangeReason DateChangeReason { get; set; } = DeliveryDateChangeReason.None;

        /// <summary>Erteleme mailine CC eklenecek seçili harici e-posta adresleri.</summary>
        public List<string>? ExtraCc { get; set; }

        public UpdateShipmentDetailsCommand() { }

        public UpdateShipmentDetailsCommand(int shipmentId, DateTime deliveryDate, List<ShipmentLineUpdateDto> lines)
        {
            ShipmentId = shipmentId;
            DeliveryDate = deliveryDate;
            Lines = lines;
        }
    }

    public class UpdateShipmentDetailsCommandValidator : AbstractValidator<UpdateShipmentDetailsCommand>
    {
        private static readonly StockUnit[] ValidUnits = (StockUnit[])Enum.GetValues(typeof(StockUnit));

        public UpdateShipmentDetailsCommandValidator()
        {
            RuleFor(x => x.ShipmentId)
                .GreaterThan(0).WithMessage("Geçerli bir sevkiyat ID'si girilmelidir.");

            RuleFor(x => x.DeliveryDate)
                .NotEmpty().WithMessage("Teslimat tarihi boş olamaz.");

            RuleFor(x => x.Lines)
                .NotNull().WithMessage("Satır listesi boş olamaz.")
                .Must(l => l.Count > 0).WithMessage("En az bir satır girilmelidir.");

            RuleForEach(x => x.Lines).ChildRules(line =>
            {
                line.RuleFor(l => l.LineId)
                    .GreaterThan(0).When(l => l.LineId.HasValue && l.LineId.Value != 0)
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
