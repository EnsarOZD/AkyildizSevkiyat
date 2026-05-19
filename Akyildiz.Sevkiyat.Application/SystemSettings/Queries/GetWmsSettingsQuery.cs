using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.SystemSettings.Queries
{
    public record GetWmsSettingsQuery : IRequest<WmsSettingsDto>;

    public record WmsSettingsDto(
        bool WmsPutawayEnabled,
        bool WmsLocationPickingEnabled,
        bool WmsBarcodePickingEnabled);

    public class GetWmsSettingsQueryHandler : IRequestHandler<GetWmsSettingsQuery, WmsSettingsDto>
    {
        private readonly IApplicationDbContext _context;
        public GetWmsSettingsQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<WmsSettingsDto> Handle(GetWmsSettingsQuery request, CancellationToken ct)
        {
            var settings = await _context.SystemSettings.FindAsync(new object[] { 1 }, ct);
            return new WmsSettingsDto(
                settings?.WmsPutawayEnabled ?? false,
                settings?.WmsLocationPickingEnabled ?? false,
                settings?.WmsBarcodePickingEnabled ?? false);
        }
    }
}
