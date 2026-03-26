using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Transport.Vehicles.Commands.UpdateVehicle
{
    public record UpdateVehicleCommand(int Id, string PlateNumber, string? Capacity, bool IsActive) : IRequest<bool>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager" };
    }

    public class UpdateVehicleCommandHandler : IRequestHandler<UpdateVehicleCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        public UpdateVehicleCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<bool> Handle(UpdateVehicleCommand request, CancellationToken cancellationToken)
        {
            var vehicle = await _context.Vehicles.FindAsync(new object[] { request.Id }, cancellationToken);
            if (vehicle == null) return false;

            vehicle.PlateNumber = request.PlateNumber;
            vehicle.Capacity = request.Capacity;
            vehicle.IsActive = request.IsActive;

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
