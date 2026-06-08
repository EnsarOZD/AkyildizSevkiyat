using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.ClothingKeywords
{
    public record ClothingKeywordDto(int Id, string Keyword, string Color, bool IsActive, int SortOrder);

    public record GetClothingKeywordsQuery(bool ActiveOnly = false) : IRequest<List<ClothingKeywordDto>>;

    public class GetClothingKeywordsQueryHandler : IRequestHandler<GetClothingKeywordsQuery, List<ClothingKeywordDto>>
    {
        private readonly IApplicationDbContext _context;
        public GetClothingKeywordsQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<List<ClothingKeywordDto>> Handle(GetClothingKeywordsQuery request, CancellationToken cancellationToken)
        {
            var q = _context.ClothingHighlightKeywords.AsQueryable();
            if (request.ActiveOnly) q = q.Where(k => k.IsActive);

            return await q
                .OrderBy(k => k.SortOrder).ThenBy(k => k.Keyword)
                .Select(k => new ClothingKeywordDto(k.Id, k.Keyword, k.Color, k.IsActive, k.SortOrder))
                .ToListAsync(cancellationToken);
        }
    }
}
