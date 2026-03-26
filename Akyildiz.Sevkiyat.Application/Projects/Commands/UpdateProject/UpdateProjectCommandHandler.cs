using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Projects.Commands.UpdateProject
{
    public class UpdateProjectCommandHandler
        : IRequestHandler<UpdateProjectCommand, Unit>
    {
        private readonly IApplicationDbContext _context;

        public UpdateProjectCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (project == null)
                throw new NotFoundException("Project", request.Id);

            project.Code = request.Code;
            project.Name = request.Name;
            project.Region = request.Region;
            project.IsActive = request.IsActive;
            project.DeliveryOrder = request.DeliveryOrder;

            await _context.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
