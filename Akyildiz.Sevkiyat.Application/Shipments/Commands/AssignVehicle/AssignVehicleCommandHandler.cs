using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.Settings;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.AssignVehicle
{
    public class AssignVehicleCommandHandler : IRequestHandler<AssignVehicleCommand, AssignVehicleResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ShipmentSettings _settings;

        public AssignVehicleCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IOptions<ShipmentSettings> settings)
        {
            _context = context;
            _currentUserService = currentUserService;
            _settings = settings.Value;
        }

        public async Task<AssignVehicleResult> Handle(AssignVehicleCommand request, CancellationToken cancellationToken)
        {
            var shipment = await _context.Shipments
                .Include(s => s.Project)
                .Include(s => s.Lines)
                .FirstOrDefaultAsync(x => x.Id == request.ShipmentId, cancellationToken)
                ?? throw new NotFoundException("Shipment", request.ShipmentId);

            if (!shipment.Project.ZoneId.HasValue)
                throw new DomainException("Sevkiyatın projesine bölge atanmamış. Lütfen önce bölge atamasını yapın.");

            if (_settings.RequireIrsaliyeNoOnDispatch && string.IsNullOrWhiteSpace(shipment.IrsaliyeNo))
                throw new DomainException(
                    "Araç ataması yapılabilmesi için irsaliye numarası girilmiş olmalıdır. " +
                    "Lütfen önce irsaliye numarasını kaydedin.");

            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.Id == request.DriverId, cancellationToken)
                ?? throw new NotFoundException("Driver", request.DriverId);

            if (!driver.IsActive)
                throw new DomainException("Seçilen şoför aktif değil.");

            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.Id == request.VehicleId, cancellationToken)
                ?? throw new NotFoundException("Vehicle", request.VehicleId);

            if (!vehicle.IsActive)
                throw new DomainException("Seçilen araç aktif değil.");

            shipment.SetDriverInfo(driver.FullName, vehicle.PlateNumber, driver.Id);
            shipment.ChangeStatus(ShipmentStatus.Dispatched, _currentUserService.UserId);

            await _context.SaveChangesAsync(cancellationToken);

            return new AssignVehicleResult(shipment.Id);
        }
    }
}
