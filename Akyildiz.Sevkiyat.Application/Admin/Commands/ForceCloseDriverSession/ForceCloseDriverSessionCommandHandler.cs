using MediatR;
using Microsoft.EntityFrameworkCore;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Application.Admin.Commands.ForceCloseDriverSession
{
    public class ForceCloseDriverSessionCommandHandler : IRequestHandler<ForceCloseDriverSessionCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public ForceCloseDriverSessionCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task Handle(ForceCloseDriverSessionCommand command, CancellationToken cancellationToken)
        {
            var session = await _context.DriverSessions
                .FirstOrDefaultAsync(ds => ds.Id == command.SessionId, cancellationToken)
                ?? throw new NotFoundException("DriverSession", command.SessionId);

            session.ForceClose(_currentUser.UserId?.ToString() ?? "unknown", command.Notes);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
