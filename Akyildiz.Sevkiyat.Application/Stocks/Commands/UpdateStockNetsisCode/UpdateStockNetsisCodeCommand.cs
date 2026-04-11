using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.Stocks.Commands.UpdateStockNetsisCode
{
    public record UpdateStockNetsisCodeCommand(int Id, string? NetsisStockCode) : IRequest;

    public class UpdateStockNetsisCodeCommandHandler : IRequestHandler<UpdateStockNetsisCodeCommand>
    {
        private readonly IApplicationDbContext _context;

        public UpdateStockNetsisCodeCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(UpdateStockNetsisCodeCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.StockMasters.FindAsync(new object[] { request.Id }, cancellationToken)
                ?? throw new NotFoundException("Stock", request.Id);

            entity.NetsisStockCode = string.IsNullOrWhiteSpace(request.NetsisStockCode)
                ? null
                : request.NetsisStockCode.Trim();

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
