using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Projects.Queries.ValidateProjectCoordinates
{
    public record ValidateProjectCoordinatesQuery(List<int> ProjectIds) : IRequest<List<ProjectCoordinateValidationDto>>;

    public class ProjectCoordinateValidationDto
    {
        public int ProjectId { get; set; }
        public string ProjectCode { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string? SystemAddress { get; set; }
        public double? RecordedLat { get; set; }
        public double? RecordedLng { get; set; }
        public double? GeocodedLat { get; set; }
        public double? GeocodedLng { get; set; }
        public double? DistanceKm { get; set; }
        public string? CityName { get; set; }       // adresten geocode edilen il (apply için)
        public string? DistrictName { get; set; }   // adresten geocode edilen ilçe (apply için)
        // "Compatible" | "Suspicious" | "Incompatible" | "NoAddress" | "NoCoordinate"
        public string Status { get; set; } = string.Empty;

        // ── Veri kalitesi ──────────────────────────────────────────────
        /// <summary>Kayıtlı koordinatın kaynağı (None/Geocoded/DriverVerified/Manual).</summary>
        public string RecordedSource { get; set; } = "None";
        public DateTime? VerifiedAt { get; set; }
        /// <summary>Projede kayıtlı il/ilçe (müşteri/ISS verisi).</summary>
        public string? StoredCityName { get; set; }
        public string? StoredDistrictName { get; set; }
        /// <summary>Tespit edilen gerçek il/ilçe (şoför-doğrulanmış koordinatın ters-geocode'u, yoksa adres geocode'u).</summary>
        public string? DetectedCityName { get; set; }
        public string? DetectedDistrictName { get; set; }
        // "Compatible" | "Mismatch" | "Unknown"
        public string CityDistrictStatus { get; set; } = "Unknown";

        // ── İsim bazlı (Google Places) çapraz kontrol ──────────────────
        /// <summary>Proje adından Places ile bulunan koordinat (yalnızca uyumsuz/şüpheli/eksik satırlarda).</summary>
        public double? PlaceLat { get; set; }
        public double? PlaceLng { get; set; }
        /// <summary>İsim-koordinatı ile kayıtlı koordinat arası mesafe (km).</summary>
        public double? PlaceDistanceKm { get; set; }
        /// <summary>İsim-koordinatı ile adres-geocode arası mesafe (km).</summary>
        public double? PlaceVsAddressKm { get; set; }
    }

    public class ValidateProjectCoordinatesQueryHandler
        : IRequestHandler<ValidateProjectCoordinatesQuery, List<ProjectCoordinateValidationDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IGeocodingService _geocoding;

        public ValidateProjectCoordinatesQueryHandler(IApplicationDbContext context, IGeocodingService geocoding)
        {
            _context = context;
            _geocoding = geocoding;
        }

        public async Task<List<ProjectCoordinateValidationDto>> Handle(
            ValidateProjectCoordinatesQuery request,
            CancellationToken cancellationToken)
        {
            if (request.ProjectIds.Count > 100)
                throw new DomainException("Tek seferde en fazla 100 proje doğrulanabilir.");

            var projects = await _context.Projects
                .Where(p => request.ProjectIds.Contains(p.Id))
                .Select(p => new
                {
                    p.Id, p.Code, p.Name, p.Address, p.Latitude, p.Longitude,
                    p.CityName, p.DistrictName, p.LocationSource, p.LocationVerifiedAt
                })
                .ToListAsync(cancellationToken);

            var results = new List<ProjectCoordinateValidationDto>();

            foreach (var project in projects)
            {
                var hasCoord = project.Latitude.HasValue && project.Longitude.HasValue;
                var trustedCoord = hasCoord
                    && (project.LocationSource == Domain.Enums.LocationSource.DriverVerified
                     || project.LocationSource == Domain.Enums.LocationSource.Manual);

                var dto = new ProjectCoordinateValidationDto
                {
                    ProjectId          = project.Id,
                    ProjectCode        = project.Code,
                    ProjectName        = project.Name,
                    SystemAddress      = project.Address,
                    RecordedLat        = project.Latitude,
                    RecordedLng        = project.Longitude,
                    RecordedSource     = project.LocationSource.ToString(),
                    VerifiedAt         = project.LocationVerifiedAt,
                    StoredCityName     = project.CityName,
                    StoredDistrictName = project.DistrictName,
                };

                // ── 1) Gerçek il/ilçe tespiti ────────────────────────────────
                // Şoför-doğrulanmış (veya manuel) koordinat varsa ters-geocode en güvenilir.
                GeocodedLocation? detected = null;
                if (trustedCoord)
                {
                    await Task.Delay(100, cancellationToken);
                    try
                    {
                        detected = await _geocoding.ReverseGeocodeAsync(
                            project.Latitude!.Value, project.Longitude!.Value, cancellationToken);
                    }
                    catch (OperationCanceledException) { throw; }
                    catch { /* sessizce geç */ }
                }

                // ── 2) Adres geocode (mesafe + adres-il/ilçe yedeği) ─────────
                GeocodedLocation? geocoded = null;
                if (!string.IsNullOrWhiteSpace(project.Address))
                {
                    await Task.Delay(100, cancellationToken);
                    try { geocoded = await _geocoding.GeocodeAsync(project.Address, cancellationToken); }
                    catch (OperationCanceledException) { throw; }
                    catch { /* sessizce geç */ }
                }

                // Adres↔koordinat mesafe durumu
                if (string.IsNullOrWhiteSpace(project.Address))
                {
                    dto.Status = "NoAddress";
                }
                else if (geocoded == null)
                {
                    dto.Status = hasCoord ? "NoAddress" : "NoCoordinate";
                }
                else
                {
                    dto.GeocodedLat  = geocoded.Lat;
                    dto.GeocodedLng  = geocoded.Lng;
                    dto.CityName     = geocoded.CityName;
                    dto.DistrictName = geocoded.DistrictName;

                    if (!hasCoord)
                    {
                        dto.Status = "NoCoordinate";
                    }
                    else
                    {
                        var km = HaversineKm(
                            project.Latitude!.Value, project.Longitude!.Value,
                            geocoded.Lat, geocoded.Lng);
                        dto.DistanceKm = Math.Round(km, 2);
                        dto.Status = km < 5 ? "Compatible" : km < 50 ? "Suspicious" : "Incompatible";
                    }
                }

                // ── 3) İl/ilçe karşılaştırması ───────────────────────────────
                // Tespit: güvenilir koordinatın ters-geocode'u; yoksa adres geocode'u.
                var detCity     = detected?.CityName     ?? geocoded?.CityName;
                var detDistrict = detected?.DistrictName ?? geocoded?.DistrictName;
                dto.DetectedCityName     = detCity;
                dto.DetectedDistrictName = detDistrict;

                if (string.IsNullOrWhiteSpace(detCity) && string.IsNullOrWhiteSpace(detDistrict))
                {
                    dto.CityDistrictStatus = "Unknown";
                }
                else
                {
                    var cityMismatch     = !string.IsNullOrWhiteSpace(detCity) && !NormEq(project.CityName, detCity);
                    var districtMismatch = !string.IsNullOrWhiteSpace(detDistrict) && !NormEq(project.DistrictName, detDistrict);
                    dto.CityDistrictStatus = (cityMismatch || districtMismatch) ? "Mismatch" : "Compatible";
                }

                // ── 4) İsim bazlı Places çapraz kontrol (yalnızca sorunlu satırlar) ──
                // Uyumlu olanlarda gerek yok; kota tasarrufu için atlanır.
                if (dto.Status != "Compatible" && !string.IsNullOrWhiteSpace(project.Name))
                {
                    await Task.Delay(100, cancellationToken);
                    GeocodedLocation? place = null;
                    try { place = await _geocoding.PlaceSearchAsync(project.Name, cancellationToken); }
                    catch (OperationCanceledException) { throw; }
                    catch { /* Places kapalı/hatalı → sessizce geç */ }

                    if (place != null)
                    {
                        dto.PlaceLat = place.Lat;
                        dto.PlaceLng = place.Lng;
                        if (hasCoord)
                            dto.PlaceDistanceKm = Math.Round(
                                HaversineKm(project.Latitude!.Value, project.Longitude!.Value, place.Lat, place.Lng), 2);
                        if (geocoded != null)
                            dto.PlaceVsAddressKm = Math.Round(
                                HaversineKm(geocoded.Lat, geocoded.Lng, place.Lat, place.Lng), 2);
                    }
                }

                results.Add(dto);
            }

            return results;
        }

        private static readonly System.Globalization.CultureInfo TrCulture = new("tr-TR");
        private static bool NormEq(string? a, string? b)
            => (a ?? "").Trim().ToLower(TrCulture) == (b ?? "").Trim().ToLower(TrCulture);

        private static double HaversineKm(double lat1, double lng1, double lat2, double lng2)
        {
            const double R = 6371.0;
            var dLat = ToRad(lat2 - lat1);
            var dLng = ToRad(lng2 - lng1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                  + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
                  * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private static double ToRad(double deg) => deg * Math.PI / 180.0;
    }
}
