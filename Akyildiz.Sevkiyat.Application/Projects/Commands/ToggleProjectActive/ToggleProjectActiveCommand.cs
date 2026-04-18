using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Projects.Commands.ToggleProjectActive
{
    public record ToggleProjectActiveCommand(int Id, bool IsActive) : IRequest<Unit>;

    public class ToggleProjectActiveCommandHandler : IRequestHandler<ToggleProjectActiveCommand, Unit>
    {
        private readonly IApplicationDbContext _context;

        public ToggleProjectActiveCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(ToggleProjectActiveCommand request, CancellationToken cancellationToken)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
                ?? throw new NotFoundException("Project", request.Id);

            project.IsActive = request.IsActive;
            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
