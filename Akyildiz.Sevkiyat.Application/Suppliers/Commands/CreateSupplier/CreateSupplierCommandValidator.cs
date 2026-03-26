using FluentValidation;

namespace Akyildiz.Sevkiyat.Application.Suppliers.Commands.CreateSupplier
{
    public sealed class CreateSupplierCommandValidator
        : AbstractValidator<CreateSupplierCommand>
    {
        public CreateSupplierCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Tedarikçi adı boş olamaz.")
                .MaximumLength(200)
                .WithMessage("Tedarikçi adı en fazla 200 karakter olabilir.");
        }
    }
}
