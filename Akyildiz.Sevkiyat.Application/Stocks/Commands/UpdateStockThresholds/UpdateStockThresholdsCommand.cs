using MediatR;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Application.Stocks.Commands.UpdateStockThresholds
{
    public record UpdateStockThresholdsCommand(
        int StockMasterId,
        decimal? MinStockQty,
        decimal? ReorderPoint
    ) : IRequest;

    public class UpdateStockThresholdsCommandHandler : IRequestHandler<UpdateStockThresholdsCommand>
    {
        private readonly IApplicationDbContext _context;

        public UpdateStockThresholdsCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(UpdateStockThresholdsCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.StockMasters.FindAsync(new object[] { request.StockMasterId }, cancellationToken);

            if (entity == null)
                throw new NotFoundException("StockMaster", request.StockMasterId);

            entity.MinStockQty = request.MinStockQty;
            entity.ReorderPoint = request.ReorderPoint;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
