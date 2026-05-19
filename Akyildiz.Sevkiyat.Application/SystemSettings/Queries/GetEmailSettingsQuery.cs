using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.SystemSettings.Queries
{
    public record GetEmailSettingsQuery : IRequest<EmailSettingsDto>;

    public record EmailSettingsDto(string? ProcurementEmailCc, string? DispatchEmailCc, bool DispatchEmailEnabled);

    public class GetEmailSettingsQueryHandler : IRequestHandler<GetEmailSettingsQuery, EmailSettingsDto>
    {
        private readonly IApplicationDbContext _context;
        public GetEmailSettingsQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<EmailSettingsDto> Handle(GetEmailSettingsQuery request, CancellationToken ct)
        {
            var settings = await _context.SystemSettings.FindAsync(new object[] { 1 }, ct);
            return new EmailSettingsDto(
                settings?.ProcurementEmailCc,
                settings?.DispatchEmailCc,
                settings?.DispatchEmailEnabled ?? true);
        }
    }
}
