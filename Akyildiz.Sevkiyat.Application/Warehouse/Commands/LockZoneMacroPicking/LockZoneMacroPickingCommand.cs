using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.LockZoneMacroPicking
{
    public record LockZoneMacroPickingCommand(int ZonePreparationId, bool Release = false)
        : IRequest<Unit>;

    public class LockZoneMacroPickingCommandHandler : IRequestHandler<LockZoneMacroPickingCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public LockZoneMacroPickingCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Unit> Handle(LockZoneMacroPickingCommand request, CancellationToken cancellationToken)
        {
            var zonePrep = await _context.ZonePreparations
                .FirstOrDefaultAsync(z => z.Id == request.ZonePreparationId, cancellationToken)
                ?? throw new NotFoundException("ZonePreparation", request.ZonePreparationId);

            if (!_currentUser.UserId.HasValue)
                throw new ForbiddenException("Kullanıcı kimliği bulunamadı.");

            if (request.Release)
            {
                zonePrep.ReleaseMacroLock();
            }
            else
            {
                if (zonePrep.IsMacroLocked(_currentUser.UserId.Value))
                    throw new ConflictException(
                        $"Bu bölge şu anda {zonePrep.MacroLockedByUserName} tarafından toplanıyor.");

                var userName = _currentUser.FullName ?? $"Kullanıcı #{_currentUser.UserId}";
                zonePrep.AcquireMacroLock(_currentUser.UserId.Value, userName);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
