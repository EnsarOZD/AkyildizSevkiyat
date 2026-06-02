using Akyildiz.Sevkiyat.Application.Common.Dtos;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Customers.Queries.GetManualCustomerById
{
    public record GetManualCustomerByIdQuery(int Id) : IRequest<CustomerDto>;

    public class GetManualCustomerByIdQueryHandler
        : IRequestHandler<GetManualCustomerByIdQuery, CustomerDto>
    {
        private readonly IApplicationDbContext _context;

        public GetManualCustomerByIdQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CustomerDto> Handle(GetManualCustomerByIdQuery request, CancellationToken cancellationToken)
        {
            var dto = await _context.Projects
                .Where(p => p.Id == request.Id && p.Source == ProjectSource.Manual)
                .Select(p => new CustomerDto(
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
                    p.DefaultContactPhone))
                .FirstOrDefaultAsync(cancellationToken);

            if (dto is null)
                throw new NotFoundException($"Manuel müşteri #{request.Id} bulunamadı.");

            return dto;
        }
    }
}
