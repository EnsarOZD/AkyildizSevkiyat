using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using FluentValidation;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.Carriers.Commands.CreateCarrier
{
    public record CreateCarrierCommand : IRequest<int>
    {
        public string Name { get; init; } = string.Empty;
        public string? Phone { get; init; }
        public string? City { get; init; }
        public List<string> Plates { get; init; } = new();
    }

    public class CreateCarrierCommandValidator : AbstractValidator<CreateCarrierCommand>
    {
        public CreateCarrierCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Phone).MaximumLength(30);
            RuleFor(x => x.City).MaximumLength(100);
            RuleForEach(x => x.Plates).MaximumLength(20);
        }
    }

    public class CreateCarrierCommandHandler : IRequestHandler<CreateCarrierCommand, int>
    {
        private readonly IApplicationDbContext _context;

        public CreateCarrierCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(CreateCarrierCommand request, CancellationToken cancellationToken)
        {
            var carrier = new Carrier
            {
                Name = request.Name.Trim(),
                Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
                City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim(),
                IsActive = true,
                Vehicles = request.Plates
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(p => p.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(p => new CarrierVehicle { PlateNumber = p, IsActive = true })
                    .ToList()
            };

            _context.Carriers.Add(carrier);
            await _context.SaveChangesAsync(cancellationToken);
            return carrier.Id;
        }
    }
}
