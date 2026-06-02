using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.Customers.Commands.CreateManualCustomer
{
    public sealed class CreateManualCustomerCommandValidator
        : AbstractValidator<CreateManualCustomerCommand>
    {
        public CreateManualCustomerCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Müşteri adı zorunludur.")
                .MaximumLength(200);

            RuleFor(x => x.NetsisCariKodu)
                .NotEmpty().WithMessage("Netsis Cari Kodu zorunludur.")
                .MaximumLength(50);

            RuleFor(x => x.NetsisTeslimCariKodu)
                .MaximumLength(50);

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90)
                .When(x => x.Latitude.HasValue)
                .WithMessage("Enlem -90 ile 90 arasında olmalıdır.");

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180)
                .When(x => x.Longitude.HasValue)
                .WithMessage("Boylam -180 ile 180 arasında olmalıdır.");
        }
    }
}
