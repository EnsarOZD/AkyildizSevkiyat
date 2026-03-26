using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Transport.Vehicles.Commands.DeleteVehicle
{
    public record DeleteVehicleCommand(int Id) : IRequest<bool>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin" };
    }

    public class DeleteVehicleCommandHandler : IRequestHandler<DeleteVehicleCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        public DeleteVehicleCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<bool> Handle(DeleteVehicleCommand request, CancellationToken cancellationToken)
        {
            var vehicle = await _context.Vehicles.FindAsync(new object[] { request.Id }, cancellationToken);
            if (vehicle == null) return false;

            // Soft Delete
            vehicle.IsActive = false;
            
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
