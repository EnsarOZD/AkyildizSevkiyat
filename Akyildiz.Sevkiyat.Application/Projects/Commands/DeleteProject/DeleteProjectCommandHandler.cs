using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Projects.Commands.DeleteProject
{
    public class DeleteProjectCommandHandler
        : IRequestHandler<DeleteProjectCommand, Unit>
    {
        private readonly IApplicationDbContext _context;

        public DeleteProjectCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (project == null)
                throw new NotFoundException("Project", request.Id);

            project.IsActive = false;

            await _context.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
