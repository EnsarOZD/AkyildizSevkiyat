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
            // Hazır: aktif, haritalanmış, henüz sevkiyata dönüşmemiş siparişler.
            // IsTransferred == true olanlar zaten sevkiyat haline getirilmiştir — badge'e dahil edilmez.
            var ready = await _context.IssOrders
                .CountAsync(o => o.IsActive
                              && o.ImportStatus == ImportStatus.Ready
                              && !o.IsTransferred, cancellationToken);

            var needsMapping = await _context.IssOrders
                .CountAsync(o => o.IsActive
                              && o.ImportStatus == ImportStatus.NeedsMapping, cancellationToken);

            var passive = await _context.IssOrders
                .CountAsync(o => !o.IsActive, cancellationToken);

            return new IssOrderCountsDto(ready, needsMapping, passive);
        }
    }
}
