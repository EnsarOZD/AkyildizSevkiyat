using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Transport.Drivers.Queries.GetDrivers
{
    public record DriverDto(int Id, string FullName, string? Phone, bool IsActive, int? UserId);

    public record GetDriversQuery : IRequest<List<DriverDto>>;

    public class GetDriversQueryHandler : IRequestHandler<GetDriversQuery, List<DriverDto>>
    {
        private readonly IApplicationDbContext _context;
        public GetDriversQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<List<DriverDto>> Handle(GetDriversQuery request, CancellationToken cancellationToken)
        {
            return await _context.Drivers
                .OrderBy(d => d.FullName)
                .Select(d => new DriverDto(d.Id, d.FullName, d.Phone, d.IsActive, d.UserId))
                .ToListAsync(cancellationToken);
        }
    }
}
