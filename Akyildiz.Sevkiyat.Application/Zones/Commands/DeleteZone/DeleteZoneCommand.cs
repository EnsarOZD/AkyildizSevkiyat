using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Zones.Commands.DeleteZone
{
    public record DeleteZoneCommand(int Id) : IRequest<Unit>;

    public class DeleteZoneCommandHandler : IRequestHandler<DeleteZoneCommand, Unit>
    {
        private readonly IApplicationDbContext _context;

        public DeleteZoneCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(DeleteZoneCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.Zones.FindAsync(new object[] { request.Id }, cancellationToken);

            if (entity == null)
            {
                throw new NotFoundException("Zone", request.Id);
            }

            _context.Zones.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
