using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.Driver.Commands.StartDriverSession
{
    public class StartDriverSessionCommandValidator : AbstractValidator<StartDriverSessionCommand>
    {
        public StartDriverSessionCommandValidator()
        {
            RuleFor(x => x.QrCode)
                .NotEmpty().WithMessage("QR kod boş olamaz.")
                .Must(qr => qr.StartsWith("AKYILDIZ_VEHICLE_"))
                .WithMessage("Geçersiz QR kodu.");

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90).WithMessage("Geçersiz enlem değeri.");

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180).WithMessage("Geçersiz boylam değeri.");
        }
    }
}
