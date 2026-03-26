using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Transport.Vehicles.Queries.GetVehicles
{
    public record VehicleDto(int Id, string PlateNumber, string? Capacity, bool IsActive);
    
    public record GetVehiclesQuery : IRequest<List<VehicleDto>>;

    public class GetVehiclesQueryHandler : IRequestHandler<GetVehiclesQuery, List<VehicleDto>>
    {
        private readonly IApplicationDbContext _context;
        public GetVehiclesQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<List<VehicleDto>> Handle(GetVehiclesQuery request, CancellationToken cancellationToken)
        {
            return await _context.Vehicles
                .OrderBy(v => v.PlateNumber)
                .Select(v => new VehicleDto(v.Id, v.PlateNumber, v.Capacity, v.IsActive))
                .ToListAsync(cancellationToken);
        }
    }
}
