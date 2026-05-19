using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.StockConsumptions.Commands.DeleteStockConsumption
{
    public record DeleteStockConsumptionCommand(int Id) : IRequest<Unit>;

    public class DeleteStockConsumptionCommandHandler : IRequestHandler<DeleteStockConsumptionCommand, Unit>
    {
        private readonly IApplicationDbContext _context;

        public DeleteStockConsumptionCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(DeleteStockConsumptionCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.StockConsumptions
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
                ?? throw new NotFoundException("StockConsumption", request.Id);

            _context.StockConsumptions.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
