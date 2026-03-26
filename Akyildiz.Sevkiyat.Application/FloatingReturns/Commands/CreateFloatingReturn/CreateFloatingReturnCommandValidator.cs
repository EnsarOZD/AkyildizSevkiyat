using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.FloatingReturns.Commands.CreateFloatingReturn
{
    public class CreateFloatingReturnCommandValidator : AbstractValidator<CreateFloatingReturnCommand>
    {
        public CreateFloatingReturnCommandValidator()
        {
            RuleFor(x => x.ReturnDate)
                .NotEmpty()
                .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
                .WithMessage("İade tarihi geçerli bir tarih olmalıdır.");

            RuleFor(x => x.Qty)
                .GreaterThan(0)
                .WithMessage("Miktar sıfırdan büyük olmalıdır.");
        }
    }
}
