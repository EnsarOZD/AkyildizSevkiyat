using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.Projects.Commands.CreateProject
{
    public sealed class CreateProjectCommandValidator
        : AbstractValidator<CreateProjectCommand>
    {
        public CreateProjectCommandValidator()
        {
            RuleFor(x => x.IssProjectCode)
                .NotEmpty()
                .WithMessage("ISS proje kodu boş olamaz.");

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Proje adı boş olamaz.");

            RuleFor(x => x.Region)
                .NotEmpty()
                .WithMessage("Bölge boş olamaz.");
        }
    }
}
