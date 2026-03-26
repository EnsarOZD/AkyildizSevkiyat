using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Orders.Queries.GetImportedOrders
{
    public record GetIssOrderCountsQuery : IRequest<IssOrderCountsDto>;

    public record IssOrderCountsDto(int ReadyCount, int NeedsMappingCount, int PassiveCount);

    public class GetIssOrderCountsQueryHandler : IRequestHandler<GetIssOrderCountsQuery, IssOrderCountsDto>
    {
        private readonly IApplicationDbContext _context;

        public GetIssOrderCountsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IssOrderCountsDto> Handle(GetIssOrderCountsQuery request, CancellationToken cancellationToken)
        {
            var stats = await _context.IssOrders
                .GroupBy(o => new { o.ImportStatus, o.IsActive })
                .Select(g => new { g.Key.ImportStatus, g.Key.IsActive, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var ready = stats.Where(x => x.IsActive && x.ImportStatus == ImportStatus.Ready).Sum(x => x.Count);
            var needsMapping = stats.Where(x => x.IsActive && x.ImportStatus == ImportStatus.NeedsMapping).Sum(x => x.Count);
            var passive = stats.Where(x => !x.IsActive).Sum(x => x.Count);

            return new IssOrderCountsDto(ready, needsMapping, passive);
        }
    }
}
