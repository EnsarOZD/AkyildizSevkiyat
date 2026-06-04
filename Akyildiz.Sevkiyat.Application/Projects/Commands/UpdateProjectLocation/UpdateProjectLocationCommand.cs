using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Projects.Commands.UpdateProjectLocation
{
    public record UpdateProjectLocationCommand(
        int ProjectId,
        double? Latitude,
        double? Longitude,
        string? CityName = null,
        string? DistrictName = null,
        LocationSource? Source = null
    ) : IRequest;

    public class UpdateProjectLocationCommandHandler : IRequestHandler<UpdateProjectLocationCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public UpdateProjectLocationCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task Handle(UpdateProjectLocationCommand request, CancellationToken cancellationToken)
        {
            var project = await _context.Projects.FindAsync(new object[] { request.ProjectId }, cancellationToken)
                ?? throw new NotFoundException("Project", request.ProjectId);

            project.Latitude     = request.Latitude;
            project.Longitude    = request.Longitude;
            // İl/ilçe yalnızca gönderildiğinde güncellenir — şoför sadece enlem/boylam
            // gönderdiğinde mevcut il/ilçe bilgisi silinmesin.
            if (!string.IsNullOrWhiteSpace(request.CityName))
                project.CityName = request.CityName;
            if (!string.IsNullOrWhiteSpace(request.DistrictName))
                project.DistrictName = request.DistrictName;

            // Koordinat kaynağı/doğrulama izleme
            if (request.Latitude.HasValue && request.Longitude.HasValue)
            {
                project.LocationSource = request.Source ?? LocationSource.Manual;
                project.LocationVerifiedAt = DateTime.UtcNow;
                project.LocationVerifiedByUserId = _currentUserService.UserId;
                // Koordinat yeniden kaydedildi → "yeniden kontrol" işareti kalkar.
                project.LocationNeedsRecheck = false;
            }
            else
            {
                // Koordinat sıfırlandı
                project.LocationSource = LocationSource.None;
                project.LocationVerifiedAt = null;
                project.LocationVerifiedByUserId = null;
                project.LocationNeedsRecheck = false;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
