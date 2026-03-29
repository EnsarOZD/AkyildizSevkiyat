using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.RouteOptimization.Dtos;

namespace Akyildiz.Sevkiyat.Application.SystemSettings.Commands
{
    public record SaveDepotSettingsCommand(
        string? DepotName,
        string? DepotAddress,
        double? DepotLatitude,
        double? DepotLongitude
    ) : IRequest<DepotSettingsDto>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager" };
    }

    public class SaveDepotSettingsCommandHandler : IRequestHandler<SaveDepotSettingsCommand, DepotSettingsDto>
    {
        private readonly IApplicationDbContext _context;
        public SaveDepotSettingsCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<DepotSettingsDto> Handle(SaveDepotSettingsCommand request, CancellationToken ct)
        {
            var settings = await _context.SystemSettings.FindAsync(new object[] { 1 }, ct);
            if (settings == null)
            {
                settings = new Domain.Entities.SystemSettings { Id = 1 };
                _context.SystemSettings.Add(settings);
            }

            settings.DepotName = request.DepotName;
            settings.DepotAddress = request.DepotAddress;
            settings.DepotLatitude = request.DepotLatitude;
            settings.DepotLongitude = request.DepotLongitude;

            await _context.SaveChangesAsync(ct);

            return new DepotSettingsDto(
                settings.DepotName,
                settings.DepotAddress,
                settings.DepotLatitude,
                settings.DepotLongitude);
        }
    }
}
