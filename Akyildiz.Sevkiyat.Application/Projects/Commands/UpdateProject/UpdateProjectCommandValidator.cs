using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.Projects.Commands.UpdateProject
{
    public sealed class UpdateProjectCommandValidator
        : AbstractValidator<UpdateProjectCommand>
    {
        public UpdateProjectCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Proje kimliği geçersiz.");

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Proje adı boş olamaz.");

            RuleFor(x => x.Code)
                .NotEmpty()
                .WithMessage("Proje kodu boş olamaz.");
        }
    }
}
