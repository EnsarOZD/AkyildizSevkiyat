using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.ClothingKeywords
{
    public record DeleteClothingKeywordCommand(int Id) : IRequest<Unit>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager" };
    }

    public class DeleteClothingKeywordCommandHandler : IRequestHandler<DeleteClothingKeywordCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        public DeleteClothingKeywordCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Unit> Handle(DeleteClothingKeywordCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.ClothingHighlightKeywords
                .FirstOrDefaultAsync(k => k.Id == request.Id, cancellationToken)
                ?? throw new NotFoundException("ClothingHighlightKeyword", request.Id);

            _context.ClothingHighlightKeywords.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
