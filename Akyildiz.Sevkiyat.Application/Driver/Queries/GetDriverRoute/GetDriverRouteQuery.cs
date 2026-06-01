using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.Shipments.Queries.GetShipmentDetail;
using Akyildiz.Sevkiyat.Domain.Entities;
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
        /// <summary>Şoförün aktif bölge hazırlık ID'si — rota sıralaması güncellemek için kullanılır.</summary>
        public int? ZonePreparationId { get; init; }
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
        /// <summary>ISS verisinden veya proje varsayılan iletişim bilgisinden çözümlenir.</summary>
        public string? ContactName { get; init; }
        public string? ContactPhone { get; init; }
        public IReadOnlyList<StopShipmentDto> Shipments { get; init; } = [];
        public bool IsFullyDelivered => Shipments.All(s => s.Status == "Delivered");
        public int TotalLineCount => Shipments.Sum(s => s.LineCount);
    }

    public record ShipmentLineDto(
        string StockCode,
        string StockName,
        decimal OrderedQty,
        string Unit,
        int Category
    );

    public class StopShipmentDto
    {
        public int Id { get; init; }
        public string? ExternalOrderNumber { get; init; }
        public string? IrsaliyeNo { get; init; }
        public string Status { get; init; } = string.Empty;
        public int LineCount { get; init; }
        public DateTime DeliveryDate { get; init; }
        public DateTime? DeliveredAt { get; init; }
        public string? DeliveryRecipient { get; init; }
        public string? DeliveryNote { get; init; }
        public string? DeliveryPhotoBase64 { get; set; }
        public string? DeliveryPhotoPath { get; init; }
        public IReadOnlyList<DeliveryPhotoDto> DeliveryPhotos { get; init; } = [];
        public IReadOnlyList<ShipmentLineDto> Lines { get; init; } = [];
    }

    // ── Handler ───────────────────────────────────────────────────────────────

    public class GetDriverRouteQueryHandler : IRequestHandler<GetDriverRouteQuery, DriverRouteDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPhotoStorageService _photos;

        public GetDriverRouteQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IPhotoStorageService photos)
        {
            _context = context;
            _currentUserService = currentUserService;
            _photos = photos;
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
                .Include(s => s.Lines).ThenInclude(l => l.StockMaster)
                .Where(s =>
                    // Yalnızca yükleme onayı verilmiş (Dispatched) veya bugün teslim edilmiş sevkiyatlar
                    s.Status == ShipmentStatus.Dispatched ||
                    (s.Status == ShipmentStatus.Delivered &&
                     s.DeliveredAt.HasValue &&
                     s.DeliveredAt.Value.Date == today));

            if (driverId.HasValue)
            {
                // 1. Önce açık session'ın zone'una bak
                var openSessionZoneId = await _context.DriverSessions
                    .Where(ds => ds.DriverId == driverId.Value && ds.Status == DriverSessionStatus.Open)
                    .Select(ds => ds.ZonePreparationId)
                    .FirstOrDefaultAsync(cancellationToken);

                // 2. Açık session yoksa en son atanan zone'u al (ZonePreparation.CreatedAt'e göre)
                int? activeZoneId = openSessionZoneId;
                if (activeZoneId == null)
                {
                    activeZoneId = await _context.ZonePreparationDrivers
                        .Where(zpd => zpd.DriverId == driverId.Value)
                        .Join(_context.ZonePreparations,
                              zpd => zpd.ZonePreparationId,
                              zp  => zp.Id,
                              (zpd, zp) => new { zpd.ZonePreparationId, zp.CreatedAt })
                        .OrderByDescending(x => x.CreatedAt)
                        .Select(x => (int?)x.ZonePreparationId)
                        .FirstOrDefaultAsync(cancellationToken);
                }

                // Yalnızca aktif zone'un sevkiyatları + doğrudan atananlar (zone'suz)
                query = query.Where(s =>
                    s.AssignedDriverId == driverId.Value ||
                    (activeZoneId.HasValue && s.ZonePreparationId == activeZoneId.Value));
            }

            var shipments = await query.ToListAsync(cancellationToken);

            // Delivery photos for all shipments in this route
            var shipmentIds = shipments.Select(s => s.Id).ToList();
            var photoRows = shipmentIds.Count == 0
                ? new List<ShipmentDeliveryPhoto>()
                : await _context.ShipmentDeliveryPhotos
                    .Where(p => shipmentIds.Contains(p.ShipmentId))
                    .OrderBy(p => p.PhotoIndex)
                    .ToListAsync(cancellationToken);
            var photoMap = photoRows
                .GroupBy(p => p.ShipmentId)
                .ToDictionary(g => g.Key, g => g.Select(p => new DeliveryPhotoDto
                {
                    Id         = p.Id,
                    PhotoUrl   = _photos.GetUrl(p.PhotoPath),
                    PhotoIndex = p.PhotoIndex,
                    TakenAt    = p.TakenAt,
                }).ToList());

            // Load ZonePreparationProject.RouteOrder for sorting
            var zoneIds = shipments
                .Where(s => s.ZonePreparationId.HasValue)
                .Select(s => s.ZonePreparationId!.Value)
                .Distinct()
                .ToList();

            var routeOrderMap = new Dictionary<(int zoneId, int projectId), int?>();
            int? primaryZoneId = null;
            if (zoneIds.Any())
            {
                primaryZoneId = zoneIds.Count == 1
                    ? zoneIds[0]
                    : shipments
                        .Where(s => s.ZonePreparationId.HasValue)
                        .GroupBy(s => s.ZonePreparationId!.Value)
                        .OrderByDescending(g => g.Count())
                        .First().Key;

                var zpps = await _context.ZonePreparationProjects
                    .Where(zpp => zoneIds.Contains(zpp.ZonePreparationId))
                    .Select(zpp => new { zpp.ZonePreparationId, zpp.ProjectId, zpp.RouteOrder })
                    .ToListAsync(cancellationToken);

                foreach (var zpp in zpps)
                    routeOrderMap[(zpp.ZonePreparationId, zpp.ProjectId)] = zpp.RouteOrder;
            }

            // Group by project, sort by ZonePreparationProject.RouteOrder when available
            var grouped = shipments
                .GroupBy(s => s.ProjectId)
                .Select(g =>
                {
                    var proj = g.First().Project;
                    int? routeOrder = null;
                    foreach (var s in g)
                    {
                        if (s.ZonePreparationId.HasValue &&
                            routeOrderMap.TryGetValue((s.ZonePreparationId.Value, proj.Id), out var ro))
                        {
                            routeOrder = ro;
                            break;
                        }
                    }
                    return new
                    {
                        Project = proj,
                        ZoneOrder = proj.Zone?.Order ?? int.MaxValue,
                        SortOrder = routeOrder ?? proj.DeliveryOrder ?? int.MaxValue,
                        Shipments = g.ToList(),
                    };
                })
                .OrderBy(g => g.ZoneOrder)
                .ThenBy(g => g.SortOrder)
                .ThenBy(g => g.Project.Name)
                .ToList();

            // Kategori gösterim sırası: Sarf → Gıda → Kıyafet → diğerleri
            static int CategoryDisplayOrder(int cat) => cat switch
            {
                2 => 1, // Sarf
                1 => 2, // Gıda
                3 => 3, // Kıyafet
                _ => 4,
            };

            var stops = grouped.Select((g, idx) =>
            {
                // Teslim alacak kişi: ISS'ten gelen boşsa proje varsayılan bilgisi
                var firstIssOrder = g.Shipments.Select(s => s.IssOrder).FirstOrDefault(o => o != null);
                var contactName  = !string.IsNullOrWhiteSpace(firstIssOrder?.TeslimAlacakKisiler)
                    ? firstIssOrder.TeslimAlacakKisiler
                    : g.Project.DefaultContactName;
                var contactPhone = !string.IsNullOrWhiteSpace(firstIssOrder?.TeslimAlacakTelefonNumaralari)
                    ? firstIssOrder.TeslimAlacakTelefonNumaralari
                    : g.Project.DefaultContactPhone;

                return new DeliveryStopDto
                {
                    StopNumber       = idx + 1,
                    ProjectId        = g.Project.Id,
                    ProjectName      = g.Project.Name,
                    ProjectAddress   = g.Project.Address,
                    ZoneName         = g.Project.Zone?.Name,
                    DeliveryOrder    = g.Project.DeliveryOrder,
                    ProjectLatitude  = g.Project.Latitude,
                    ProjectLongitude = g.Project.Longitude,
                    ContactName      = contactName,
                    ContactPhone     = contactPhone,
                    Shipments        = g.Shipments.Select(s => new StopShipmentDto
                    {
                        Id                  = s.Id,
                        ExternalOrderNumber = s.IssOrder?.ExternalOrderNumber,
                        IrsaliyeNo          = s.IrsaliyeNo,
                        Status              = s.Status.ToString(),
                        LineCount           = s.Lines.Count,
                        DeliveryDate        = s.DeliveryDate,
                        DeliveredAt         = s.DeliveredAt,
                        DeliveryRecipient   = s.DeliveryRecipient,
                        DeliveryNote        = s.DeliveryNote,
                        DeliveryPhotoBase64 = s.DeliveryPhotoBase64,
                        DeliveryPhotoPath   = s.DeliveryPhotoPath,
                        DeliveryPhotos      = photoMap.TryGetValue(s.Id, out var ph) ? ph : new List<DeliveryPhotoDto>(),
                        Lines               = s.Lines
                            .OrderBy(l => CategoryDisplayOrder((int)(l.StockMaster?.Category ?? 0)))
                            .ThenBy(l => l.StockName)
                            .Select(l => new ShipmentLineDto(
                                l.StockCode,
                                l.StockName,
                                l.OrderedQty,
                                l.Unit.ToString(),
                                (int)(l.StockMaster?.Category ?? 0)
                            )).ToList(),
                    }).ToList(),
                };
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
                ZonePreparationId  = primaryZoneId,
            };
        }
    }
}
