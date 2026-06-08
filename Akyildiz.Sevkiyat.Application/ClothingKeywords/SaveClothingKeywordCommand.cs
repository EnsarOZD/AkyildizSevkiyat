using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.ClothingKeywords
{
    // Id null/0 → yeni kayıt, aksi halde güncelleme
    public record SaveClothingKeywordCommand(int? Id, string Keyword, string Color, bool IsActive, int SortOrder)
        : IRequest<int>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager" };
    }

    public class SaveClothingKeywordCommandValidator : AbstractValidator<SaveClothingKeywordCommand>
    {
        public SaveClothingKeywordCommandValidator()
        {
            RuleFor(x => x.Keyword).NotEmpty().WithMessage("Anahtar kelime zorunludur.").MaximumLength(100);
            RuleFor(x => x.Color).NotEmpty().MaximumLength(20);
        }
    }

    public class SaveClothingKeywordCommandHandler : IRequestHandler<SaveClothingKeywordCommand, int>
    {
        private readonly IApplicationDbContext _context;
        public SaveClothingKeywordCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<int> Handle(SaveClothingKeywordCommand request, CancellationToken cancellationToken)
        {
            ClothingHighlightKeyword entity;
            if (request.Id is > 0)
            {
                entity = await _context.ClothingHighlightKeywords
                    .FirstOrDefaultAsync(k => k.Id == request.Id, cancellationToken)
                    ?? throw new NotFoundException("ClothingHighlightKeyword", request.Id.Value);
            }
            else
            {
                entity = new ClothingHighlightKeyword();
                _context.ClothingHighlightKeywords.Add(entity);
            }

            entity.Keyword = request.Keyword.Trim();
            entity.Color = request.Color.Trim();
            entity.IsActive = request.IsActive;
            entity.SortOrder = request.SortOrder;

            await _context.SaveChangesAsync(cancellationToken);
            return entity.Id;
        }
    }
}
