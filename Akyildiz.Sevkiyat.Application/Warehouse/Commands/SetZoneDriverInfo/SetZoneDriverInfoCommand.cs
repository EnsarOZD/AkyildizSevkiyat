using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.Warehouse.Services;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.SetZoneDriverInfo
{
    public record SetZoneDriverInfoCommand : IRequest<bool>, IRequireRoles
    {
        public int ZonePreparationId { get; init; }
        public int DriverId { get; init; }
        public int VehicleId { get; init; }

        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Dispatcher", "Warehouse" };
    }

    public class SetZoneDriverInfoCommandHandler : IRequestHandler<SetZoneDriverInfoCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly PreDispatchGuard _preDispatchGuard;

        public SetZoneDriverInfoCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            PreDispatchGuard preDispatchGuard)
        {
            _context = context;
            _currentUserService = currentUserService;
            _preDispatchGuard = preDispatchGuard;
        }

        public async Task<bool> Handle(SetZoneDriverInfoCommand request, CancellationToken cancellationToken)
        {
            var zp = await _context.ZonePreparations
                .FirstOrDefaultAsync(z => z.Id == request.ZonePreparationId, cancellationToken);
            
            if (zp == null) throw new NotFoundException("ZonePreparation", request.ZonePreparationId);

            // Guard 1: Macro toplama tamamlanmış olmalı
            if (zp.Status < ZonePreparationStatus.ReadyForDriverInfo)
                throw new DomainException("Şoför atayabilmek için macro toplama aşamasının tamamlanmış olması gerekir.");

            // Guard 2: Hazırlık başlatılmış (frozen) olmalı
            if (!zp.IsFrozen)
                throw new DomainException("Hazırlık başlatılmadan şoför ataması yapılamaz.");

            // Guard 3: İrsaliye çekilmiş olmalı
            if (!zp.IrsaliyeFetched)
                throw new DomainException("Araca atamadan önce Netsisten irsaliye numaraları çekilmelidir.");

            // Guard 4: Tüm aktif sevkiyatlar ReadyForDispatch statüsünde olmalı
            await _preDispatchGuard.ThrowIfShipmentsNotReadyAsync(request.ZonePreparationId, cancellationToken);

            // Guard 5: Zone genelinde açık reconciliation hatası olmamalı
            await _preDispatchGuard.ThrowIfZoneHasOpenErrorsAsync(request.ZonePreparationId, cancellationToken);

            zp.DriverId = request.DriverId;
            zp.VehicleId = request.VehicleId;

            var driver = await _context.Drivers.FindAsync(new object[] { request.DriverId }, cancellationToken)
                ?? throw new NotFoundException("Driver", request.DriverId);

            var vehicle = await _context.Vehicles.FindAsync(new object[] { request.VehicleId }, cancellationToken)
                ?? throw new NotFoundException("Vehicle", request.VehicleId);

            string driverName  = driver.FullName;
            string plateNumber = vehicle.PlateNumber;
            
            // Validate Status? 
            // Only allow if ReadyForDriverInfo or MicroReady?
            // Let's assume valid state is ReadyForDriverInfo or later (to update)
            
            // Advance status if not yet Transferred
            if (zp.Status == ZonePreparationStatus.ReadyForDriverInfo)
            {
                zp.Status = ZonePreparationStatus.ReadyForTransfer;
            }

            // Sync Shipment Statuses to AssignedToVehicle
            var shipments = await _context.Shipments
                .Include(s => s.Project)
                .Where(s =>
                    s.ZonePreparationId == zp.Id &&
                    s.Status != ShipmentStatus.Cancelled &&
                    s.Status != ShipmentStatus.Passive &&
                    s.Status != ShipmentStatus.Created && // Do not touch Drafts
                    s.Status < ShipmentStatus.Dispatched  // Do not re-process dispatched shipments
                )
                .ToListAsync(cancellationToken);

            foreach(var s in shipments)
            {
                s.ChangeStatus(ShipmentStatus.AssignedToVehicle, _currentUserService.UserId);
                s.SetDriverInfo(driverName, plateNumber, request.DriverId);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
