using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Stocks.Queries.GetUnmappedStocks
{
    public record UnmappedStockDto(int MappingId, string ExternalCode, string ExternalName);

    public record GetUnmappedStocksQuery() : IRequest<List<UnmappedStockDto>>;

    public class GetUnmappedStocksQueryHandler : IRequestHandler<GetUnmappedStocksQuery, List<UnmappedStockDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetUnmappedStocksQueryHandler(IApplicationDbContext context)
        {
             _context = context;
        }

        public async Task<List<UnmappedStockDto>> Handle(GetUnmappedStocksQuery request, CancellationToken cancellationToken)
        {
            return await _context.StockMappings
                .Where(m => m.MatchStatus == MatchStatus.Unmapped && m.ExternalSystem == "ISS-IP")
                .Select(m => new UnmappedStockDto(m.Id, m.ExternalStockCode, m.ExternalStockName))
                .ToListAsync(cancellationToken);
        }
    }
}
