using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.Customers.Commands.UpdateManualCustomer
{
    public sealed class UpdateManualCustomerCommandValidator
        : AbstractValidator<UpdateManualCustomerCommand>
    {
        public UpdateManualCustomerCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);

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
                .When(x => x.Latitude.HasValue);

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180)
                .When(x => x.Longitude.HasValue);
        }
    }
}
