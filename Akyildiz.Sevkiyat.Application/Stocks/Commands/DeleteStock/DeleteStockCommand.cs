using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using System;

using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Application.Stocks.Commands.DeleteStock
{
    public record DeleteStockCommand(int Id) : IRequest<Unit>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager" };
    }

    public class DeleteStockCommandHandler : IRequestHandler<DeleteStockCommand, Unit>
    {
        private readonly IApplicationDbContext _context;

        public DeleteStockCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(DeleteStockCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.StockMasters.FindAsync(new object[] { request.Id }, cancellationToken);

            if (entity == null)
            {
                throw new NotFoundException("Stock", request.Id);
            }

            _context.StockMasters.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
