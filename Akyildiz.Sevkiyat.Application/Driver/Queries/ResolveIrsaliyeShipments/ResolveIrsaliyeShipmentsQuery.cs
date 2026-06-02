using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Driver.Queries.ResolveIrsaliyeShipments
{
    /// <summary>
    /// Sefer başlatmadan önce: araç QR'ı + okutulan irsaliye no'dan o seferin sevkiyat
    /// listesini (salt-okunur) çözer. Şoför listeyi görüp onaylar, sonra sefer başlatılır.
    /// </summary>
    public record ResolveIrsaliyeShipmentsQuery(string QrCode, string IrsaliyeNo)
        : IRequest<ResolveIrsaliyeShipmentsResult>;

    public record ResolveIrsaliyeShipmentsResult(
        string VehiclePlateNumber,
        List<TripShipmentDto> Shipments
    );

    public record TripShipmentDto(
        int Id,
        string ProjectName,
        string? TalepNo,
        string? IrsaliyeNo,
        string Status,
        int LineCount
    );

    public class ResolveIrsaliyeShipmentsQueryHandler
        : IRequestHandler<ResolveIrsaliyeShipmentsQuery, ResolveIrsaliyeShipmentsResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public ResolveIrsaliyeShipmentsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<ResolveIrsaliyeShipmentsResult> Handle(
            ResolveIrsaliyeShipmentsQuery request,
            CancellationToken cancellationToken)
        {
            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.UserId == _currentUser.UserId && d.IsActive, cancellationToken)
                ?? throw new NotFoundException("Şoför kaydı bulunamadı.");

            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.QrCode == request.QrCode, cancellationToken)
                ?? throw new DomainException("Geçersiz araç QR kodu.");

            var today = DateTime.UtcNow.Date;

            var zoneAssignment = await _context.ZonePreparationDrivers
                .Include(zpd => zpd.ZonePreparation)
                .FirstOrDefaultAsync(zpd =>
                    zpd.DriverId == driver.Id &&
                    zpd.ZonePreparation.VehicleId == vehicle.Id &&
                    zpd.ZonePreparation.DeliveryDate.Date >= today &&
                    zpd.ZonePreparation.Status <= ZonePreparationStatus.Dispatched,
                    cancellationToken);

            var tripQuery = zoneAssignment != null
                ? _context.Shipments.Where(s =>
                    s.ZonePreparationId == zoneAssignment.ZonePreparationId &&
                    (s.Status == ShipmentStatus.AssignedToVehicle || s.Status == ShipmentStatus.Dispatched))
                : _context.Shipments.Where(s =>
                    s.AssignedDriverId == driver.Id &&
                    s.AssignedPlateNumber == vehicle.PlateNumber &&
                    (s.Status == ShipmentStatus.AssignedToVehicle || s.Status == ShipmentStatus.Dispatched));

            var tripShipments = await tripQuery
                .Include(s => s.Project)
                .Select(s => new
                {
                    s.Id,
                    ProjectName = s.Project.Name,
                    s.TalepNo,
                    s.IrsaliyeNo,
                    s.Status,
                    LineCount = s.Lines.Count
                })
                .ToListAsync(cancellationToken);

            if (tripShipments.Count == 0)
                throw new DomainException("Bu araca atanmış aktif sevkiyat bulunamadı.");

            var scanned = NormalizeIrsaliye(request.IrsaliyeNo);
            if (string.IsNullOrEmpty(scanned))
                throw new DomainException("İrsaliye numarası okunamadı.");

            if (!tripShipments.Any(s => NormalizeIrsaliye(s.IrsaliyeNo) == scanned))
                throw new DomainException(
                    "Okuttuğunuz irsaliye bu araca atanmış sevkiyatlarla eşleşmiyor.");

            return new ResolveIrsaliyeShipmentsResult(
                vehicle.PlateNumber,
                tripShipments
                    .Select(s => new TripShipmentDto(
                        s.Id, s.ProjectName, s.TalepNo, s.IrsaliyeNo, s.Status.ToString(), s.LineCount))
                    .ToList());
        }

        private static string NormalizeIrsaliye(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            return new string(raw.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        }
    }
}
