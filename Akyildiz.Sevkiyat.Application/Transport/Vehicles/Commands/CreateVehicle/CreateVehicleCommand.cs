using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Transport.Vehicles.Commands.CreateVehicle
{
    public record CreateVehicleCommand(
        string PlateNumber,
        string? Capacity,
        VehicleType VehicleType = VehicleType.Kamyon,
        string? Description = null
    ) : IRequest<int>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager" };
    }

    public class CreateVehicleCommandHandler : IRequestHandler<CreateVehicleCommand, int>
    {
        private readonly IApplicationDbContext _context;
        public CreateVehicleCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<int> Handle(CreateVehicleCommand request, CancellationToken cancellationToken)
        {
            var vehicle = new Vehicle
            {
                PlateNumber = request.PlateNumber,
                Capacity    = request.Capacity,
                VehicleType = request.VehicleType,
                Description = request.Description,
                IsActive    = true
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync(cancellationToken);
            return vehicle.Id;
        }
    }
}
