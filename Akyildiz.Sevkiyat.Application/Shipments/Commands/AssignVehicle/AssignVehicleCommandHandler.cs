using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.Settings;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using DriverEntity = Akyildiz.Sevkiyat.Domain.Entities.Driver;

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
                .FirstOrDefaultAsync(x => x.Id == request.ShipmentId, cancellationToken);

            if (shipment == null)
                throw new NotFoundException("Shipment", request.ShipmentId);

            if (!shipment.Project.ZoneId.HasValue)
                throw new DomainException("Shipment's project does not have an assigned zone. Please assign a zone first.");

            if (_settings.RequireIrsaliyeNoOnDispatch && string.IsNullOrWhiteSpace(shipment.IrsaliyeNo))
                throw new DomainException(
                    "Araç ataması yapılabilmesi için irsaliye numarası girilmiş olmalıdır. " +
                    "Lütfen önce irsaliye numarasını kaydedin.");

            // Driver IsActive check — look up driver by name
            DriverEntity? driver = null;
            int activeCount = 0;

            if (!string.IsNullOrWhiteSpace(request.DriverName))
            {
                driver = await _context.Drivers
                    .FirstOrDefaultAsync(d => d.FullName == request.DriverName, cancellationToken);

                if (driver is not null)
                {
                    if (!driver.IsActive)
                        throw new DomainException("Bu sürücü aktif değil.");

                    // Count other active shipments assigned to this driver for today
                    activeCount = await _context.Shipments
                        .CountAsync(s => s.AssignedDriverId == driver.Id
                            && s.Id != request.ShipmentId
                            && (s.Status == ShipmentStatus.AssignedToVehicle || s.Status == ShipmentStatus.Dispatched)
                            && s.DeliveryDate.Date == DateTime.UtcNow.Date,
                            cancellationToken);
                }
                // If no Driver record found by name, skip IsActive check — DriverName is a free-text field
            }

            shipment.SetDriverInfo(request.DriverName, request.PlateNumber, driver?.Id);

            shipment.ChangeStatus(ShipmentStatus.AssignedToVehicle, _currentUserService.UserId);

            // Stok çıkışı araç atamasında DEĞİL, teslimatta (MarkShipmentDelivered) yapılır.
            // Araçtaki mal fiziksel olarak depodadır — teslim onayına kadar OnHandQty değişmez.

            await _context.SaveChangesAsync(cancellationToken);

            DriverWarning? warning = null;
            if (activeCount > 0)
                warning = new DriverWarning(activeCount,
                    $"Bu sürücünün bugün {activeCount} aktif sevkiyatı daha var.");

            return new AssignVehicleResult(shipment.Id, warning);
        }
    }
}
