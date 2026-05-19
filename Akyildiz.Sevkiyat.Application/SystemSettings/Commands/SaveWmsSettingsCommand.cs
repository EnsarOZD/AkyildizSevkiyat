using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.SystemSettings.Queries;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.SystemSettings.Commands
{
    public record SaveWmsSettingsCommand(
        bool WmsPutawayEnabled,
        bool WmsLocationPickingEnabled,
        bool WmsBarcodePickingEnabled)
        : IRequest<WmsSettingsDto>;

    public class SaveWmsSettingsCommandHandler : IRequestHandler<SaveWmsSettingsCommand, WmsSettingsDto>
    {
        private readonly IApplicationDbContext _context;
        public SaveWmsSettingsCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<WmsSettingsDto> Handle(SaveWmsSettingsCommand request, CancellationToken ct)
        {
            var settings = await _context.SystemSettings.FindAsync(new object[] { 1 }, ct);
            if (settings == null)
            {
                settings = new Domain.Entities.SystemSettings { Id = 1 };
                _context.SystemSettings.Add(settings);
            }

            settings.WmsPutawayEnabled         = request.WmsPutawayEnabled;
            settings.WmsLocationPickingEnabled  = request.WmsLocationPickingEnabled;
            settings.WmsBarcodePickingEnabled   = request.WmsBarcodePickingEnabled;

            await _context.SaveChangesAsync(ct);
            return new WmsSettingsDto(
                settings.WmsPutawayEnabled,
                settings.WmsLocationPickingEnabled,
                settings.WmsBarcodePickingEnabled);
        }
    }
}
