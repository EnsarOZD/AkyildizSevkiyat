using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.Auth.Commands.Login
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(v => v.Email)
                .NotEmpty().WithMessage("E-posta adresi gereklidir.")
                .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.");

            RuleFor(v => v.Password)
                .NotEmpty().WithMessage("Şifre gereklidir.");
        }
    }
}
