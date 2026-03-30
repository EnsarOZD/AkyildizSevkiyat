using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Application.Driver.Queries.GetDriverRoute
{
    /// <summary>
    /// Şoför rotasını teslimat noktası bazında döner.
    /// Bir proje = bir teslimat noktası. Bir noktada N irsaliye olabilir.
    /// Sıralama: Zone.Order ASC → Project.DeliveryOrder ASC (null en sona) → ProjectName ASC
    /// </summary>
    public record GetDriverRouteQuery : IRequest<DriverRouteDto>;

    // ── DTOs ─────────────────────────────────────────────────────────────────

    public class DriverRouteDto
    {
        public IReadOnlyList<DeliveryStopDto> Stops { get; init; } = [];
        public int TotalStops { get; init; }
        public int CompletedStops { get; init; }
        public int TotalShipments { get; init; }
        public int CompletedShipments { get; init; }
        /// <summary>Google Maps Directions URL ile tüm duraklar sıralı.</summary>
        public string? MapsRouteUrl { get; init; }
    }

    public class DeliveryStopDto
    {
        public int StopNumber { get; init; }
        public int ProjectId { get; init; }
        public string ProjectName { get; init; } = string.Empty;
        public string? ProjectAddress { get; init; }
        public string? ZoneName { get; init; }
        public int? DeliveryOrder { get; init; }
        public double? ProjectLatitude { get; init; }
        public double? ProjectLongitude { get; init; }
        public IReadOnlyList<StopShipmentDto> Shipments { get; init; } = [];
        public bool IsFullyDelivered => Shipments.All(s => s.Status == "Delivered");
        public int TotalLineCount => Shipments.Sum(s => s.LineCount);
    }

    public record ShipmentLineDto(
        string StockCode,
        string StockName,
        decimal OrderedQty,
        string Unit
    );

    public class StopShipmentDto
    {
        public int Id { get; init; }
        public string? TalepNo { get; init; }
        public string? IrsaliyeNo { get; init; }
        public string Status { get; init; } = string.Empty;
        public int LineCount { get; init; }
        public string? TeslimAlacakKisiler { get; init; }
        public string? TeslimAlacakTelefon { get; init; }
        public DateTime? DeliveredAt { get; init; }
        public string? DeliveryRecipient { get; init; }
        public string? DeliveryNote { get; init; }
        public string? DeliveryPhotoBase64 { get; set; }
        public IReadOnlyList<ShipmentLineDto> Lines { get; init; } = [];
    }

    // ── Handler ───────────────────────────────────────────────────────────────

    public class GetDriverRouteQueryHandler : IRequestHandler<GetDriverRouteQuery, DriverRouteDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetDriverRouteQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<DriverRouteDto> Handle(
            GetDriverRouteQuery request,
            CancellationToken cancellationToken)
        {
            var today = DateTime.UtcNow.Date;

            int? driverId = null;
            if (_currentUserService.Role == UserRole.Driver)
            {
                var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == _currentUserService.UserId, cancellationToken)
                    ?? throw new ForbiddenException("Kullanıcıya tanımlı bir şoför kaydı bulunamadı.");
                driverId = driver.Id;
            }

            var query = _context.Shipments
                .Include(s => s.Project).ThenInclude(p => p.Zone)
                .Include(s => s.IssOrder)
                .Include(s => s.Lines)
                .Where(s =>
                    s.Status == ShipmentStatus.AssignedToVehicle ||
                    s.Status == ShipmentStatus.Dispatched ||
                    (s.Status == ShipmentStatus.Delivered &&
                     s.DeliveredAt.HasValue &&
                     s.DeliveredAt.Value.Date == today));

            if (driverId.HasValue)
            {
                // Şoföre direkt atanan sevkiyatlar + çoklu şoför tablosundan atanan zone'lar
                var assignedZoneIds = await _context.ZonePreparationDrivers
                    .Where(zpd => zpd.DriverId == driverId.Value)
                    .Select(zpd => zpd.ZonePreparationId)
                    .ToListAsync(cancellationToken);

                query = query.Where(s =>
                    s.AssignedDriverId == driverId.Value ||
                    (s.ZonePreparationId.HasValue && assignedZoneIds.Contains(s.ZonePreparationId.Value)));
            }

            var shipments = await query.ToListAsync(cancellationToken);

            // Group by project
            var grouped = shipments
                .GroupBy(s => s.ProjectId)
                .Select(g =>
                {
                    var proj = g.First().Project;
                    return new
                    {
                        Project = proj,
                        ZoneOrder = proj.Zone?.Order ?? int.MaxValue,
                        DeliveryOrder = proj.DeliveryOrder ?? int.MaxValue,
                        Shipments = g.ToList(),
                    };
                })
                .OrderBy(g => g.ZoneOrder)
                .ThenBy(g => g.DeliveryOrder)
                .ThenBy(g => g.Project.Name)
                .ToList();

            var stops = grouped.Select((g, idx) => new DeliveryStopDto
            {
                StopNumber    = idx + 1,
                ProjectId     = g.Project.Id,
                ProjectName   = g.Project.Name,
                ProjectAddress = g.Project.Address,
                ZoneName         = g.Project.Zone?.Name,
                DeliveryOrder    = g.Project.DeliveryOrder,
                ProjectLatitude  = g.Project.Latitude,
                ProjectLongitude = g.Project.Longitude,
                Shipments        = g.Shipments.Select(s => new StopShipmentDto
                {
                    Id                  = s.Id,
                    TalepNo             = s.TalepNo ?? s.IssOrder?.TalepNo,
                    IrsaliyeNo          = s.IrsaliyeNo,
                    Status              = s.Status.ToString(),
                    LineCount           = s.Lines.Count,
                    TeslimAlacakKisiler = s.IssOrder?.TeslimAlacakKisiler,
                    TeslimAlacakTelefon = s.IssOrder?.TeslimAlacakTelefonNumaralari,
                    DeliveredAt         = s.DeliveredAt,
                    DeliveryRecipient   = s.DeliveryRecipient,
                    DeliveryNote        = s.DeliveryNote,
                    DeliveryPhotoBase64 = s.DeliveryPhotoBase64,
                    Lines               = s.Lines.Select(l => new ShipmentLineDto(
                        l.StockCode,
                        l.StockName,
                        l.OrderedQty,
                        l.Unit.ToString()
                    )).ToList(),
                }).ToList(),
            }).ToList();

            // Google Maps multi-stop URL — starts from current location ("My+Location")
            // Uses lat/lng if available, otherwise address string
            var destinations = stops
                .Where(s => !s.IsFullyDelivered)
                .Select(s =>
                {
                    if (s.ProjectLatitude.HasValue && s.ProjectLongitude.HasValue)
                        return Uri.EscapeDataString($"{s.ProjectLatitude.Value},{s.ProjectLongitude.Value}");
                    if (!string.IsNullOrWhiteSpace(s.ProjectAddress))
                        return Uri.EscapeDataString(s.ProjectAddress!);
                    return null;
                })
                .Where(d => d != null)
                .ToList();

            string? mapsUrl = destinations.Count > 0
                ? $"https://www.google.com/maps/dir/My+Location/{string.Join("/", destinations)}"
                : null;

            return new DriverRouteDto
            {
                Stops              = stops,
                TotalStops         = stops.Count,
                CompletedStops     = stops.Count(s => s.IsFullyDelivered),
                TotalShipments     = shipments.Count,
                CompletedShipments = shipments.Count(s => s.Status == ShipmentStatus.Delivered),
                MapsRouteUrl       = mapsUrl,
            };
        }
    }
}
