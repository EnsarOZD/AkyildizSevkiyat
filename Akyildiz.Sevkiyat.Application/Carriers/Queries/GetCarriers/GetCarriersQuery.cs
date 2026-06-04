using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Carriers.Queries.GetCarriers
{
    public class GetCarriersQuery : IRequest<List<CarrierDto>>
    {
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
    }

    public class CarrierVehicleDto
    {
        public int Id { get; set; }
        public string PlateNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class CarrierDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? City { get; set; }
        public bool IsActive { get; set; }
        public List<CarrierVehicleDto> Vehicles { get; set; } = new();
    }

    public class GetCarriersQueryHandler : IRequestHandler<GetCarriersQuery, List<CarrierDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetCarriersQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CarrierDto>> Handle(GetCarriersQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Carriers.Include(c => c.Vehicles).AsQueryable();

            if (request.IsActive.HasValue)
                query = query.Where(c => c.IsActive == request.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = request.Search.Trim();
                query = query.Where(c =>
                    EF.Functions.Collate(c.Name, "Turkish_CI_AS").Contains(EF.Functions.Collate(term, "Turkish_CI_AS")) ||
                    (c.City != null && EF.Functions.Collate(c.City, "Turkish_CI_AS").Contains(EF.Functions.Collate(term, "Turkish_CI_AS"))) ||
                    c.Vehicles.Any(v => v.PlateNumber.Contains(term)));
            }

            return await query
                .OrderBy(c => c.Name)
                .Select(c => new CarrierDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Phone = c.Phone,
                    City = c.City,
                    IsActive = c.IsActive,
                    Vehicles = c.Vehicles
                        .OrderBy(v => v.PlateNumber)
                        .Select(v => new CarrierVehicleDto
                        {
                            Id = v.Id,
                            PlateNumber = v.PlateNumber,
                            IsActive = v.IsActive
                        }).ToList()
                })
                .ToListAsync(cancellationToken);
        }
    }
}
