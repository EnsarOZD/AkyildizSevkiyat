using Akyildiz.Sevkiyat.Application.RouteOptimization.Dtos;

namespace Akyildiz.Sevkiyat.Application.SystemSettings.Queries
{
    public record GetDepotSettingsQuery : IRequest<DepotSettingsDto>;

    public class GetDepotSettingsQueryHandler : IRequestHandler<GetDepotSettingsQuery, DepotSettingsDto>
    {
        private readonly IApplicationDbContext _context;
        public GetDepotSettingsQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<DepotSettingsDto> Handle(GetDepotSettingsQuery request, CancellationToken ct)
        {
            var settings = await _context.SystemSettings.FindAsync(new object[] { 1 }, ct);
            if (settings == null)
                return new DepotSettingsDto(null, null, null, null);

            return new DepotSettingsDto(
                settings.DepotName,
                settings.DepotAddress,
                settings.DepotLatitude,
                settings.DepotLongitude);
        }
    }
}
