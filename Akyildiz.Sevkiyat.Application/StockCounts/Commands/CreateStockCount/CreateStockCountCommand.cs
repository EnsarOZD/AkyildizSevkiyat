using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.StockCounts.Commands.CreateStockCount
{
    /// <summary>
    /// Yeni sayım formu oluşturur. Aktif tüm stok kalemleri ExpectedQty (OnHandQty snapshot)
    /// ile otomatik eklenir. ActualQty başlangıçta null gelir — depocu doldurur.
    /// </summary>
    public record CreateStockCountCommand(
        DateTime CountDate,
        string? Note
    ) : IRequest<int>;

    public class CreateStockCountCommandValidator : AbstractValidator<CreateStockCountCommand>
    {
        public CreateStockCountCommandValidator()
        {
            RuleFor(x => x.CountDate)
                .NotEmpty()
                .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
                .WithMessage("Sayım tarihi geçerli bir tarih olmalıdır.");
        }
    }

    public class CreateStockCountCommandHandler : IRequestHandler<CreateStockCountCommand, int>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CreateStockCountCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<int> Handle(CreateStockCountCommand request, CancellationToken cancellationToken)
        {
            // Guard: aktif (Draft) sayım varsa yeni sayım başlatma
            var existingDraft = await _context.StockCounts
                .AnyAsync(sc => sc.Status == StockCountStatus.Draft, cancellationToken);
            if (existingDraft)
                throw new Domain.Exceptions.DomainException("Devam eden bir sayım zaten mevcut. Önce mevcut sayımı tamamlayın veya iptal edin.");

            // Snapshot: tüm aktif stok kalemleri
            var stocks = await _context.StockMasters
                .Where(s => s.IsActive)
                .OrderBy(s => s.StockCode)
                .ToListAsync(cancellationToken);

            var stockCount = new StockCount
            {
                CountDate = request.CountDate,
                Note = request.Note,
                CreatedByUserId = _currentUserService.UserId,
                CreatedAt = DateTime.UtcNow,
                Lines = stocks.Select(s => new StockCountLine
                {
                    StockMasterId = s.Id,
                    ExpectedQty = s.OnHandQty,
                    ActualQty = null
                }).ToList()
            };

            _context.StockCounts.Add(stockCount);
            await _context.SaveChangesAsync(cancellationToken);

            return stockCount.Id;
        }
    }
}
