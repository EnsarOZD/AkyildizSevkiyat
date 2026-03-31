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
        // "Compatible" | "Suspicious" | "Incompatible" | "NoAddress" | "NoCoordinate"
        public string Status { get; set; } = string.Empty;
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
                .Select(p => new { p.Id, p.Code, p.Name, p.Address, p.Latitude, p.Longitude })
                .ToListAsync(cancellationToken);

            var results = new List<ProjectCoordinateValidationDto>();

            foreach (var project in projects)
            {
                var dto = new ProjectCoordinateValidationDto
                {
                    ProjectId  = project.Id,
                    ProjectCode = project.Code,
                    ProjectName = project.Name,
                    SystemAddress = project.Address,
                    RecordedLat = project.Latitude,
                    RecordedLng = project.Longitude,
                };

                if (string.IsNullOrWhiteSpace(project.Address))
                {
                    dto.Status = "NoAddress";
                    results.Add(dto);
                    continue;
                }

                // Rate limiting: Google kotası için 100ms bekleme (≤10 istek/saniye)
                await Task.Delay(100, cancellationToken);

                try
                {
                    var geocoded = await _geocoding.GeocodeAsync(project.Address, cancellationToken);

                    if (geocoded == null)
                    {
                        // Adres var ama geocode edilemedi
                        dto.Status = project.Latitude == null ? "NoCoordinate" : "NoAddress";
                        results.Add(dto);
                        continue;
                    }

                    dto.GeocodedLat = geocoded.Value.Lat;
                    dto.GeocodedLng = geocoded.Value.Lng;

                    if (project.Latitude == null || project.Longitude == null)
                    {
                        // Kayıtlı koordinat yok — geocoded koordinat döndürüldü, frontend kaydedebilir
                        dto.Status = "NoCoordinate";
                    }
                    else
                    {
                        var km = HaversineKm(
                            project.Latitude.Value, project.Longitude.Value,
                            geocoded.Value.Lat, geocoded.Value.Lng);

                        dto.DistanceKm = Math.Round(km, 2);
                        dto.Status = km < 5 ? "Compatible"
                            : km < 50 ? "Suspicious"
                            : "Incompatible";
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    dto.Status = project.Latitude == null ? "NoCoordinate" : "NoAddress";
                }

                results.Add(dto);
            }

            return results;
        }

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
