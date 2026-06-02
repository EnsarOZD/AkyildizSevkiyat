using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Customers.Commands.UpdateManualCustomer
{
    public class UpdateManualCustomerCommandHandler
        : IRequestHandler<UpdateManualCustomerCommand>
    {
        private readonly IApplicationDbContext _context;

        public UpdateManualCustomerCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(UpdateManualCustomerCommand request, CancellationToken cancellationToken)
        {
            var customer = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == request.Id && p.Source == ProjectSource.Manual, cancellationToken);

            if (customer is null)
                throw new NotFoundException($"Manuel müşteri #{request.Id} bulunamadı.");

            var trimmedCari = request.NetsisCariKodu.Trim();

            var duplicateCari = await _context.Projects
                .AnyAsync(p => p.Id != request.Id
                            && p.Source == ProjectSource.Manual
                            && p.NetsisCariKodu == trimmedCari, cancellationToken);

            if (duplicateCari)
                throw new ConflictException(
                    $"Netsis Cari Kodu '{trimmedCari}' başka bir manuel müşteri için zaten tanımlı.");

            customer.Name = request.Name.Trim();
            customer.OperationType = request.OperationType;
            customer.NetsisCariKodu = trimmedCari;
            customer.NetsisTeslimCariKodu = string.IsNullOrWhiteSpace(request.NetsisTeslimCariKodu)
                ? null : request.NetsisTeslimCariKodu.Trim();
            customer.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
            customer.CityName = string.IsNullOrWhiteSpace(request.CityName) ? null : request.CityName.Trim();
            customer.DistrictName = string.IsNullOrWhiteSpace(request.DistrictName) ? null : request.DistrictName.Trim();
            customer.Latitude = request.Latitude;
            customer.Longitude = request.Longitude;
            customer.DefaultContactName = string.IsNullOrWhiteSpace(request.DefaultContactName)
                ? null : request.DefaultContactName.Trim();
            customer.DefaultContactPhone = string.IsNullOrWhiteSpace(request.DefaultContactPhone)
                ? null : request.DefaultContactPhone.Trim();

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
