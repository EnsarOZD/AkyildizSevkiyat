using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.Auth.Commands.Login
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(v => v.Username)
                .NotEmpty().WithMessage("Kullanıcı adı gereklidir.")
                .MaximumLength(200);

            RuleFor(v => v.Password)
                .NotEmpty().WithMessage("Şifre gereklidir.");
        }
    }
}
