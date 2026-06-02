using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.Warehouse.Services;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.FetchZoneIrsaliye
{
    public record FetchZoneIrsaliyeCommand(int ZonePreparationId) : IRequest<FetchZoneIrsaliyeResult>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting", "Warehouse", "Driver" };
    }

    public record FetchZoneIrsaliyeResult(int Fetched, int Skipped, List<string> Errors, List<string> Warnings);

    public class FetchZoneIrsaliyeCommandHandler : IRequestHandler<FetchZoneIrsaliyeCommand, FetchZoneIrsaliyeResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ZoneIrsaliyeFetcher _fetcher;

        public FetchZoneIrsaliyeCommandHandler(IApplicationDbContext context, ZoneIrsaliyeFetcher fetcher)
        {
            _context = context;
            _fetcher = fetcher;
        }

        public async Task<FetchZoneIrsaliyeResult> Handle(FetchZoneIrsaliyeCommand request, CancellationToken cancellationToken)
        {
            var zp = await _context.ZonePreparations
                .FirstOrDefaultAsync(z => z.Id == request.ZonePreparationId, cancellationToken)
                ?? throw new NotFoundException("ZonePreparation", request.ZonePreparationId);

            if (zp.Status != ZonePreparationStatus.ReadyForDriverInfo)
                throw new DomainException("İrsaliye çekimi yalnızca 'Sevke Hazır' aşamasındaki hazırlıklar için yapılabilir.");

            return await _fetcher.FetchAsync(request.ZonePreparationId, cancellationToken);
        }
    }
}
