using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Zones.Commands.UpdateZone
{
    public record UpdateZoneCommand(int Id, string Name, int Order) : IRequest<Unit>;

    public class UpdateZoneCommandHandler : IRequestHandler<UpdateZoneCommand, Unit>
    {
        private readonly IApplicationDbContext _context;

        public UpdateZoneCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(UpdateZoneCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.Zones.FindAsync(new object[] { request.Id }, cancellationToken);

            if (entity == null)
            {
                throw new NotFoundException("Zone", request.Id);
            }

            entity.Name = request.Name;
            entity.Order = request.Order;

            await _context.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
