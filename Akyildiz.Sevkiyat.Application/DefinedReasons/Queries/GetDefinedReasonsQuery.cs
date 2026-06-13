using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.DefinedReasons.Queries
{
    public record DefinedReasonDto(int Id, ReasonCategory Category, string Label, int SortOrder, bool IsActive);

    public record GetDefinedReasonsQuery(ReasonCategory? Category = null, bool? ActiveOnly = null)
        : IRequest<List<DefinedReasonDto>>;

    public class GetDefinedReasonsQueryHandler : IRequestHandler<GetDefinedReasonsQuery, List<DefinedReasonDto>>
    {
        private readonly IApplicationDbContext _context;
        public GetDefinedReasonsQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<List<DefinedReasonDto>> Handle(GetDefinedReasonsQuery request, CancellationToken cancellationToken)
        {
            var q = _context.DefinedReasons.AsQueryable();
            if (request.Category.HasValue)
                q = q.Where(x => x.Category == request.Category.Value);
            if (request.ActiveOnly == true)
                q = q.Where(x => x.IsActive);

            return await q
                .OrderBy(x => x.Category)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.Label)
                .Select(x => new DefinedReasonDto(x.Id, x.Category, x.Label, x.SortOrder, x.IsActive))
                .ToListAsync(cancellationToken);
        }
    }
}
