using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Users.Commands.ToggleUserActive
{
    public record ToggleUserActiveCommand(int Id, bool IsActive) : IRequest;

    public class ToggleUserActiveCommandHandler : IRequestHandler<ToggleUserActiveCommand>
    {
        private readonly IApplicationDbContext _context;

        public ToggleUserActiveCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(ToggleUserActiveCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken)
                ?? throw new NotFoundException($"Kullanıcı bulunamadı (Id: {request.Id}).");

            user.SetActive(request.IsActive);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
