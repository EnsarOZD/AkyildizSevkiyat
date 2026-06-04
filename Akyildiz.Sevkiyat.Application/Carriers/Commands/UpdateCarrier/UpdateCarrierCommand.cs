using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Carriers.Commands.UpdateCarrier
{
    public record UpdateCarrierCommand : IRequest
    {
        public int Id { get; set; }
        public string Name { get; init; } = string.Empty;
        public string? Phone { get; init; }
        public string? City { get; init; }
        public bool IsActive { get; init; } = true;
        /// <summary>İstenen tam plaka listesi — eksikler silinir, yeniler eklenir.</summary>
        public List<string> Plates { get; init; } = new();
    }

    public class UpdateCarrierCommandValidator : AbstractValidator<UpdateCarrierCommand>
    {
        public UpdateCarrierCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Phone).MaximumLength(30);
            RuleFor(x => x.City).MaximumLength(100);
            RuleForEach(x => x.Plates).MaximumLength(20);
        }
    }

    public class UpdateCarrierCommandHandler : IRequestHandler<UpdateCarrierCommand>
    {
        private readonly IApplicationDbContext _context;

        public UpdateCarrierCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(UpdateCarrierCommand request, CancellationToken cancellationToken)
        {
            var carrier = await _context.Carriers
                .Include(c => c.Vehicles)
                .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
                ?? throw new NotFoundException("Carrier", request.Id);

            carrier.Name = request.Name.Trim();
            carrier.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
            carrier.City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim();
            carrier.IsActive = request.IsActive;

            var desiredPlates = request.Plates
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Kaldırılacaklar: artık istenmeyen plakalar
            var toRemove = carrier.Vehicles
                .Where(v => !desiredPlates.Contains(v.PlateNumber, StringComparer.OrdinalIgnoreCase))
                .ToList();
            if (toRemove.Count > 0)
                _context.CarrierVehicles.RemoveRange(toRemove);

            // Eklenecekler: mevcut olmayan yeni plakalar
            var existingPlates = carrier.Vehicles
                .Select(v => v.PlateNumber)
                .ToList();
            foreach (var plate in desiredPlates)
            {
                if (!existingPlates.Contains(plate, StringComparer.OrdinalIgnoreCase))
                    carrier.Vehicles.Add(new CarrierVehicle { CarrierId = carrier.Id, PlateNumber = plate, IsActive = true });
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
