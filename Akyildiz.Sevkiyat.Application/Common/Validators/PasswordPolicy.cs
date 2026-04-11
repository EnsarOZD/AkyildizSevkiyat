using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.Common.Validators;

public static class PasswordPolicy
{
    public const int MinLength = 8;

    /// <summary>
    /// En az 8 karakter, bir büyük harf, bir rakam ve bir özel karakter zorunlu.
    /// </summary>
    public static IRuleBuilderOptions<T, string> MustBeValidPassword<T>(
        this IRuleBuilder<T, string> rule)
    {
        return rule
            .NotEmpty().WithMessage("Şifre boş olamaz.")
            .MinimumLength(MinLength).WithMessage($"Şifre en az {MinLength} karakter olmalıdır.")
            .Matches("[A-Z]").WithMessage("Şifre en az bir büyük harf içermelidir.")
            .Matches("[0-9]").WithMessage("Şifre en az bir rakam içermelidir.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Şifre en az bir özel karakter içermelidir (örn. !, @, #, _).");
    }
}
