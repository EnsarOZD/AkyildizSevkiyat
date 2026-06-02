using System.Globalization;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Customers.Commands.CreateManualCustomer
{
    public class CreateManualCustomerCommandHandler
        : IRequestHandler<CreateManualCustomerCommand, int>
    {
        private const string ManualCodePrefix = "MM-";
        private const int CodePadding = 4;

        private readonly IApplicationDbContext _context;

        public CreateManualCustomerCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(CreateManualCustomerCommand request, CancellationToken cancellationToken)
        {
            var trimmedName = request.Name.Trim();
            var trimmedCari = request.NetsisCariKodu.Trim();
            var trimmedTeslim = string.IsNullOrWhiteSpace(request.NetsisTeslimCariKodu)
                ? null : request.NetsisTeslimCariKodu.Trim();

            var duplicateCari = await _context.Projects
                .AnyAsync(p => p.Source == ProjectSource.Manual
                            && p.NetsisCariKodu == trimmedCari, cancellationToken);

            if (duplicateCari)
                throw new ConflictException(
                    $"Netsis Cari Kodu '{trimmedCari}' başka bir manuel müşteri için zaten tanımlı.");

            var code = await GenerateNextCodeAsync(cancellationToken);

            var customer = new Project
            {
                Code = code,
                Name = trimmedName,
                Source = ProjectSource.Manual,
                OperationType = request.OperationType,
                IsActive = true,
                NetsisCariKodu = trimmedCari,
                NetsisTeslimCariKodu = trimmedTeslim,
                Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
                CityName = string.IsNullOrWhiteSpace(request.CityName) ? null : request.CityName.Trim(),
                DistrictName = string.IsNullOrWhiteSpace(request.DistrictName) ? null : request.DistrictName.Trim(),
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                DefaultContactName = string.IsNullOrWhiteSpace(request.DefaultContactName)
                    ? null : request.DefaultContactName.Trim(),
                DefaultContactPhone = string.IsNullOrWhiteSpace(request.DefaultContactPhone)
                    ? null : request.DefaultContactPhone.Trim()
            };

            _context.Projects.Add(customer);
            await _context.SaveChangesAsync(cancellationToken);

            return customer.Id;
        }

        /// <summary>
        /// Manuel müşteri için sıradaki kodu üretir. MM-XXXX formatında, mevcut en yüksek
        /// numaralı manuel kodun +1 fazlası. Race koşulunda DB unique index hata verir;
        /// commit tarafı SaveChanges'te yakalanır.
        /// </summary>
        private async Task<string> GenerateNextCodeAsync(CancellationToken cancellationToken)
        {
            var existingCodes = await _context.Projects
                .Where(p => p.Source == ProjectSource.Manual && p.Code.StartsWith(ManualCodePrefix))
                .Select(p => p.Code)
                .ToListAsync(cancellationToken);

            int max = 0;
            foreach (var c in existingCodes)
            {
                var tail = c.Substring(ManualCodePrefix.Length);
                if (int.TryParse(tail, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n > max)
                    max = n;
            }

            return ManualCodePrefix + (max + 1).ToString("D" + CodePadding, CultureInfo.InvariantCulture);
        }
    }
}
