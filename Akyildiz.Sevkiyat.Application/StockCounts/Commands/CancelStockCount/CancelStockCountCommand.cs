using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.StockCounts.Commands.CancelStockCount
{
    public record CancelStockCountCommand(int StockCountId) : IRequest;

    public class CancelStockCountCommandHandler : IRequestHandler<CancelStockCountCommand>
    {
        private readonly IApplicationDbContext _context;

        public CancelStockCountCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(CancelStockCountCommand request, CancellationToken cancellationToken)
        {
            var stockCount = await _context.StockCounts
                .FirstOrDefaultAsync(sc => sc.Id == request.StockCountId, cancellationToken);

            if (stockCount == null)
                throw new NotFoundException("Sayım kaydı bulunamadı.");

            if (stockCount.Status != StockCountStatus.Draft)
                throw new DomainException("Sadece devam eden (Draft) sayımlar iptal edilebilir.");

            stockCount.Status = StockCountStatus.Cancelled;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
