using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.StockCounts.Commands.UpdateStockCountLines
{
    /// <summary>
    /// Depocu sayım satırlarına ActualQty girer. Sayım Draft durumunda olmalıdır.
    /// Toplu güncelleme: birden fazla satır tek seferde gönderilebilir.
    /// </summary>
    public record UpdateStockCountLinesCommand(
        int StockCountId,
        List<CountLineUpdateDto> Lines
    ) : IRequest<Unit>;

    public record CountLineUpdateDto(
        int StockCountLineId,
        decimal ActualQty,
        string? Note
    );

    public class UpdateStockCountLinesCommandValidator : AbstractValidator<UpdateStockCountLinesCommand>
    {
        public UpdateStockCountLinesCommandValidator()
        {
            RuleFor(x => x.StockCountId).GreaterThan(0);
            RuleFor(x => x.Lines).NotEmpty();
            RuleForEach(x => x.Lines).ChildRules(l =>
            {
                l.RuleFor(x => x.StockCountLineId).GreaterThan(0);
                l.RuleFor(x => x.ActualQty).GreaterThanOrEqualTo(0).WithMessage("Sayım miktarı sıfır veya daha büyük olmalıdır.");
            });
        }
    }

    public class UpdateStockCountLinesCommandHandler : IRequestHandler<UpdateStockCountLinesCommand, Unit>
    {
        private readonly IApplicationDbContext _context;

        public UpdateStockCountLinesCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(UpdateStockCountLinesCommand request, CancellationToken cancellationToken)
        {
            var stockCount = await _context.StockCounts
                .Include(c => c.Lines)
                .FirstOrDefaultAsync(c => c.Id == request.StockCountId, cancellationToken);

            if (stockCount == null)
                throw new NotFoundException("StockCount", request.StockCountId);

            if (stockCount.Status != Domain.Entities.StockCountStatus.Draft)
                throw new DomainException("Tamamlanmış sayımlarda değişiklik yapılamaz.");

            var lineMap = stockCount.Lines.ToDictionary(l => l.Id);

            foreach (var dto in request.Lines)
            {
                if (!lineMap.TryGetValue(dto.StockCountLineId, out var line))
                    throw new DomainException($"Satır #{dto.StockCountLineId} bu sayıma ait değil.");

                line.ActualQty = dto.ActualQty;
                if (!string.IsNullOrWhiteSpace(dto.Note))
                    line.Note = dto.Note;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
