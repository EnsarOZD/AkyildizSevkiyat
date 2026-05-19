using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.WarehouseLocations.Commands.DeleteWarehouseLocation
{
    public record BulkDeleteWarehouseLocationsCommand(List<int> Ids) : IRequest<int>;

    public class BulkDeleteWarehouseLocationsCommandHandler : IRequestHandler<BulkDeleteWarehouseLocationsCommand, int>
    {
        private readonly IApplicationDbContext _context;

        public BulkDeleteWarehouseLocationsCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(BulkDeleteWarehouseLocationsCommand request, CancellationToken cancellationToken)
        {
            var entities = await _context.WarehouseLocations
                .Where(l => request.Ids.Contains(l.Id))
                .ToListAsync(cancellationToken);

            if (entities.Count == 0) return 0;

            _context.WarehouseLocations.RemoveRange(entities);
            await _context.SaveChangesAsync(cancellationToken);
            return entities.Count;
        }
    }
}
