using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.WarehouseLocations.Commands.DeleteWarehouseLocation
{
    public record DeleteWarehouseLocationCommand(int Id) : IRequest;

    public class DeleteWarehouseLocationCommandHandler : IRequestHandler<DeleteWarehouseLocationCommand>
    {
        private readonly IApplicationDbContext _context;

        public DeleteWarehouseLocationCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(DeleteWarehouseLocationCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.WarehouseLocations
                .FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken)
                ?? throw new NotFoundException("Lokasyon bulunamadı.");

            _context.WarehouseLocations.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
