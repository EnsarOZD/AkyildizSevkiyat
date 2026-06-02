using Akyildiz.Sevkiyat.Application.Common.Dtos;
using Akyildiz.Sevkiyat.Application.Common.Models;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Customers.Queries.GetManualCustomers
{
    public record GetManualCustomersQuery(
        int PageNumber = 1,
        int PageSize = 50,
        string? Search = null,
        bool ShowInactive = false
    ) : IRequest<PaginatedList<CustomerDto>>;

    public class GetManualCustomersQueryHandler
        : IRequestHandler<GetManualCustomersQuery, PaginatedList<CustomerDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetManualCustomersQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedList<CustomerDto>> Handle(GetManualCustomersQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Projects
                .Where(p => p.Source == ProjectSource.Manual)
                .AsQueryable();

            if (!request.ShowInactive)
                query = query.Where(p => p.IsActive);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var s = request.Search.Trim();
                query = query.Where(p =>
                    EF.Functions.Collate(p.Code, "Turkish_CI_AS").Contains(EF.Functions.Collate(s, "Turkish_CI_AS")) ||
                    EF.Functions.Collate(p.Name, "Turkish_CI_AS").Contains(EF.Functions.Collate(s, "Turkish_CI_AS")) ||
                    (p.NetsisCariKodu != null && EF.Functions.Collate(p.NetsisCariKodu, "Turkish_CI_AS").Contains(EF.Functions.Collate(s, "Turkish_CI_AS"))));
            }

            var projected = query.OrderByDescending(p => p.Id).Select(p => new CustomerDto(
                p.Id,
                p.Code,
                p.Name,
                p.IsActive,
                p.OperationType,
                p.NetsisCariKodu,
                p.NetsisTeslimCariKodu,
                p.Address,
                p.CityName,
                p.DistrictName,
                p.Latitude,
                p.Longitude,
                p.DefaultContactName,
                p.DefaultContactPhone
            ));

            return await PaginatedList<CustomerDto>.CreateAsync(projected, request.PageNumber, request.PageSize);
        }
    }
}
