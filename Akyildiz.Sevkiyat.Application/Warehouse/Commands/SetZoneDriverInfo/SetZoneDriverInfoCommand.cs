using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.RouteOptimization.Services;
using Akyildiz.Sevkiyat.Application.Warehouse.Services;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.SetZoneDriverInfo
{
    // ── Response ──────────────────────────────────────────────────────────────

    public record SetZoneDriverInfoResult(
        bool Success,
        bool OptimizationApplied,
        string? OptimizationWarning);

    // ── Command ───────────────────────────────────────────────────────────────

    public record SetZoneDriverInfoCommand : IRequest<SetZoneDriverInfoResult>, IRequireRoles
    {
        public int ZonePreparationId { get; init; }

        /// <summary>En az 1 eleman. İlk eleman ana şoför (IsPrimary=true).</summary>
        public List<int> DriverIds { get; init; } = new();

        public int VehicleId { get; init; }

        /// <summary>Hareket saati — null ise 08:00 varsayılır.</summary>
        public TimeOnly? DepartureTime { get; init; }

        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Dispatcher", "Warehouse" };
    }

    // ── Validator ─────────────────────────────────────────────────────────────

    public class SetZoneDriverInfoCommandValidator : AbstractValidator<SetZoneDriverInfoCommand>
    {
        public SetZoneDriverInfoCommandValidator()
        {
            RuleFor(x => x.ZonePreparationId).GreaterThan(0);
            RuleFor(x => x.VehicleId).GreaterThan(0);
            RuleFor(x => x.DriverIds)
                .NotEmpty().WithMessage("En az bir şoför seçilmelidir.")
                .Must(ids => ids.Distinct().Count() == ids.Count)
                .WithMessage("Aynı şoför birden fazla seçilemez.");
            RuleForEach(x => x.DriverIds).GreaterThan(0);
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────────

    public class SetZoneDriverInfoCommandHandler
        : IRequestHandler<SetZoneDriverInfoCommand, SetZoneDriverInfoResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly PreDispatchGuard _preDispatchGuard;
        private readonly ILogger<SetZoneDriverInfoCommandHandler> _logger;

        public SetZoneDriverInfoCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            PreDispatchGuard preDispatchGuard,
            ILogger<SetZoneDriverInfoCommandHandler> logger)
        {
            _context           = context;
            _currentUserService = currentUserService;
            _preDispatchGuard  = preDispatchGuard;
            _logger            = logger;
        }

        public async Task<SetZoneDriverInfoResult> Handle(
            SetZoneDriverInfoCommand request,
            CancellationToken cancellationToken)
        {
            // ── Guards ────────────────────────────────────────────────────────

            var zp = await _context.ZonePreparations
                .FirstOrDefaultAsync(z => z.Id == request.ZonePreparationId, cancellationToken)
                ?? throw new NotFoundException("ZonePreparation", request.ZonePreparationId);

            if (zp.Status < ZonePreparationStatus.ReadyForDriverInfo)
                throw new DomainException("Şoför atayabilmek için macro toplama aşamasının tamamlanmış olması gerekir.");

            if (!zp.IsFrozen)
                throw new DomainException("Hazırlık başlatılmadan şoför ataması yapılamaz.");

            if (!zp.IrsaliyeFetched)
                throw new DomainException("Araca atamadan önce Netsisten irsaliye numaraları çekilmelidir.");

            await _preDispatchGuard.ThrowIfShipmentsNotReadyAsync(request.ZonePreparationId, cancellationToken);
            await _preDispatchGuard.ThrowIfZoneHasOpenErrorsAsync(request.ZonePreparationId, cancellationToken);

            // ── Validate drivers exist ────────────────────────────────────────

            var drivers = await _context.Drivers
                .Where(d => request.DriverIds.Contains(d.Id))
                .ToListAsync(cancellationToken);

            var missing = request.DriverIds.Except(drivers.Select(d => d.Id)).ToList();
            if (missing.Any())
                throw new NotFoundException("Driver", missing.First());

            var vehicle = await _context.Vehicles
                .FindAsync(new object[] { request.VehicleId }, cancellationToken)
                ?? throw new NotFoundException("Vehicle", request.VehicleId);

            // ── Primary driver info ───────────────────────────────────────────

            var primaryDriverId = request.DriverIds[0];
            var primaryDriver   = drivers.First(d => d.Id == primaryDriverId);
            string driverName  = primaryDriver.FullName;
            string plateNumber = vehicle.PlateNumber;

            // ── Rota optimizasyonu (non-blocking) ─────────────────────────────

            bool   optimizationApplied  = false;
            string? optimizationWarning = null;
            try
            {
                optimizationApplied = await ApplyRouteOptimizationAsync(
                    zp, vehicle, request.DepartureTime, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Rota optimizasyonu başarısız, mevcut sıralama korundu.");
                optimizationWarning = "Rota optimize edilemedi, mevcut sıralama korundu.";
            }

            // ── ZonePreparationDrivers tablosunu güncelle ─────────────────────

            var existingAssignments = await _context.ZonePreparationDrivers
                .Where(zpd => zpd.ZonePreparationId == zp.Id)
                .ToListAsync(cancellationToken);

            _context.ZonePreparationDrivers.RemoveRange(existingAssignments);

            for (int i = 0; i < request.DriverIds.Count; i++)
            {
                _context.ZonePreparationDrivers.Add(new ZonePreparationDriver
                {
                    ZonePreparationId = zp.Id,
                    DriverId          = request.DriverIds[i],
                    IsPrimary         = (i == 0)
                });
            }

            // ── ZonePreparation güncelle ──────────────────────────────────────

            zp.DriverId  = primaryDriverId;
            zp.VehicleId = request.VehicleId;

            if (zp.Status == ZonePreparationStatus.ReadyForDriverInfo)
                zp.Status = ZonePreparationStatus.ReadyForTransfer;

            // ── Shipments statüsü güncelle ────────────────────────────────────

            var shipments = await _context.Shipments
                .Include(s => s.Project)
                .Where(s =>
                    s.ZonePreparationId == zp.Id &&
                    s.Status != ShipmentStatus.Cancelled &&
                    s.Status != ShipmentStatus.Passive &&
                    s.Status != ShipmentStatus.Created &&
                    s.Status < ShipmentStatus.Dispatched)
                .ToListAsync(cancellationToken);

            foreach (var s in shipments)
            {
                s.ChangeStatus(ShipmentStatus.AssignedToVehicle, _currentUserService.UserId);
                s.SetDriverInfo(driverName, plateNumber, primaryDriverId);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return new SetZoneDriverInfoResult(true, optimizationApplied, optimizationWarning);
        }

        // ── Rota optimizasyonu ────────────────────────────────────────────────

        private async Task<bool> ApplyRouteOptimizationAsync(
            ZonePreparation zp,
            Vehicle vehicle,
            TimeOnly? departureTime,
            CancellationToken cancellationToken)
        {
            var zpProjects = await _context.ZonePreparationProjects
                .Include(zpp => zpp.Project)
                .Where(zpp => zpp.ZonePreparationId == zp.Id)
                .ToListAsync(cancellationToken);

            var realProjects = zpProjects
                .Where(zpp => zpp.Project != null)
                .ToList();

            if (realProjects.Count < 2)
                return false;  // tek durak, sıralamaya gerek yok

            var depot = await _context.SystemSettings
                .FirstOrDefaultAsync(cancellationToken);

            string vehicleTypeStr = vehicle.VehicleType switch
            {
                VehicleType.Kamyon   => "Kamyon",
                VehicleType.Kamyonet => "Kamyonet",
                VehicleType.Minibus  => "Minibüs",
                _                    => "Kamyon"
            };

            var stops = realProjects.Select(zpp => new RouteOrderingService.StopInfo(
                zpp.Project.Id.ToString(),
                zpp.Project.Name,
                zpp.Project.Address ?? string.Empty,
                zpp.Project.Latitude,
                zpp.Project.Longitude,
                null,
                null)).ToList();

            var ordering = new RouteOrderingService();
            var ordered  = ordering.OrderStops(
                stops,
                depot?.DepotLatitude,
                depot?.DepotLongitude,
                vehicleTypeStr,
                false,
                true,
                departureTime ?? new TimeOnly(8, 0));

            // Extract non-bridge stops in order → map ProjectId → DeliveryOrder
            var orderMap = ordered.Stops
                .Where(s => s.Code != "__BRIDGE__")
                .Select((s, idx) => (ProjectId: int.Parse(s.Code), Order: idx + 1))
                .ToDictionary(x => x.ProjectId, x => x.Order);

            foreach (var zpp in realProjects)
            {
                if (orderMap.TryGetValue(zpp.Project.Id, out var order))
                    zpp.Project.DeliveryOrder = order;
            }

            return true;
        }
    }
}
