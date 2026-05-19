using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.SystemSettings.Queries;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.SystemSettings.Commands
{
    public record SaveEmailSettingsCommand(string? ProcurementEmailCc, string? DispatchEmailCc, bool DispatchEmailEnabled)
        : IRequest<EmailSettingsDto>;

    public class SaveEmailSettingsCommandHandler : IRequestHandler<SaveEmailSettingsCommand, EmailSettingsDto>
    {
        private readonly IApplicationDbContext _context;
        public SaveEmailSettingsCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<EmailSettingsDto> Handle(SaveEmailSettingsCommand request, CancellationToken ct)
        {
            var settings = await _context.SystemSettings.FindAsync(new object[] { 1 }, ct);
            if (settings == null)
            {
                settings = new Domain.Entities.SystemSettings { Id = 1 };
                _context.SystemSettings.Add(settings);
            }

            settings.ProcurementEmailCc   = NullIfEmpty(request.ProcurementEmailCc);
            settings.DispatchEmailCc       = NullIfEmpty(request.DispatchEmailCc);
            settings.DispatchEmailEnabled  = request.DispatchEmailEnabled;

            await _context.SaveChangesAsync(ct);
            return new EmailSettingsDto(settings.ProcurementEmailCc, settings.DispatchEmailCc, settings.DispatchEmailEnabled);
        }

        private static string? NullIfEmpty(string? s) =>
            string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }
}
