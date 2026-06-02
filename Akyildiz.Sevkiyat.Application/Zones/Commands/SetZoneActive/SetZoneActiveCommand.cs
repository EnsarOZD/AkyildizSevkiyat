using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Zones.Commands.SetZoneActive
{
    /// <summary>
    /// Bölgeyi pasife alır veya yeniden aktif eder (soft-delete).
    /// Fiziksel silinemeyen (hazırlık kayıtları olan) bölgeler için kullanılır.
    /// </summary>
    public record SetZoneActiveCommand(int Id, bool IsActive) : IRequest<Unit>;

    public class SetZoneActiveCommandHandler : IRequestHandler<SetZoneActiveCommand, Unit>
    {
        private readonly IApplicationDbContext _context;

        public SetZoneActiveCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(SetZoneActiveCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.Zones.FindAsync(new object[] { request.Id }, cancellationToken);

            if (entity == null)
            {
                throw new NotFoundException("Zone", request.Id);
            }

            entity.IsActive = request.IsActive;
            await _context.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
