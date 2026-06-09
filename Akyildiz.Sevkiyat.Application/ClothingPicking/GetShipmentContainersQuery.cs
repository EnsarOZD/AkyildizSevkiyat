using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.ClothingPicking
{
    public record ShipmentContainerDto(
        int ContainerAssignmentId, int ContainerId, string Code, int ContainerType, DateTime AssignedAt);

    /// <summary>Bir sevkiyata AÇIK (boşaltılmamış) bağlı arabaları döner.</summary>
    public record GetShipmentContainersQuery(int ShipmentId) : IRequest<List<ShipmentContainerDto>>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager", "Accounting", "Warehouse" };
    }

    public class GetShipmentContainersQueryHandler : IRequestHandler<GetShipmentContainersQuery, List<ShipmentContainerDto>>
    {
        private readonly IApplicationDbContext _context;
        public GetShipmentContainersQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<List<ShipmentContainerDto>> Handle(GetShipmentContainersQuery request, CancellationToken ct)
            => await _context.ContainerAssignments
                .Where(a => a.ShipmentId == request.ShipmentId && a.ReleasedAt == null)
                .Include(a => a.Container)
                .OrderBy(a => a.AssignedAt)
                .Select(a => new ShipmentContainerDto(
                    a.Id, a.ContainerId, a.Container.Code, (int)a.Container.Type, a.AssignedAt))
                .ToListAsync(ct);
    }
}
