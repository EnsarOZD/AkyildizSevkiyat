using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.LockProjectPicking
{
    public record LockProjectPickingCommand(int ZonePreparationProjectId, bool Release = false)
        : IRequest<Unit>;

    public class LockProjectPickingCommandHandler : IRequestHandler<LockProjectPickingCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public LockProjectPickingCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Unit> Handle(LockProjectPickingCommand request, CancellationToken cancellationToken)
        {
            var zpProject = await _context.ZonePreparationProjects
                .FirstOrDefaultAsync(p => p.Id == request.ZonePreparationProjectId, cancellationToken)
                ?? throw new NotFoundException("ZonePreparationProject", request.ZonePreparationProjectId);

            if (!_currentUser.UserId.HasValue)
                throw new ForbiddenException("Kullanıcı kimliği bulunamadı.");

            if (request.Release)
            {
                // Sadece kilidi koyan kullanıcı veya admin serbest bırakabilir
                zpProject.ReleaseLock();
            }
            else
            {
                if (zpProject.IsPickingLocked(_currentUser.UserId.Value))
                    throw new ConflictException(
                        $"Bu proje şu anda {zpProject.PickingLockedByUserName} tarafından toplanıyor.");

                var userName = _currentUser.FullName ?? $"Kullanıcı #{_currentUser.UserId}";
                zpProject.AcquireLock(_currentUser.UserId.Value, userName);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
