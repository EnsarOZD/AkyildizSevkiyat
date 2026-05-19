using FluentValidation;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.Driver.Commands.EndDriverSession
{
    public record EndDriverSessionCommand(
        string QrCode,
        double Latitude,
        double Longitude,
        string? EndOdometerPhotoBase64 = null,
        int? EndOdometerKm = null
    ) : IRequest<EndDriverSessionResult>;

    public record EndDriverSessionResult(
        Guid SessionId,
        int TotalDurationMinutes
    );

    public class EndDriverSessionCommandValidator : AbstractValidator<EndDriverSessionCommand>
    {
        public EndDriverSessionCommandValidator()
        {
            RuleFor(x => x.QrCode)
                .NotEmpty().WithMessage("QR kod boş olamaz.")
                .Must(qr => qr.StartsWith("AKYILDIZ_VEHICLE_"))
                .WithMessage("Geçersiz QR kodu.");

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90).WithMessage("Geçersiz enlem değeri.");

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180).WithMessage("Geçersiz boylam değeri.");

            RuleFor(x => x.EndOdometerPhotoBase64)
                .NotEmpty().WithMessage("Bitiş kadran fotoğrafı zorunludur.");

            RuleFor(x => x.EndOdometerKm)
                .NotNull().WithMessage("Bitiş kilometre bilgisi zorunludur.")
                .GreaterThan(0).When(x => x.EndOdometerKm.HasValue).WithMessage("Kilometre değeri 0'dan büyük olmalıdır.");
        }
    }
}
