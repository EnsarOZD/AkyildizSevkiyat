using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Projects.Commands.UpdateProjectZone
{
    public record UpdateProjectZoneCommand(int ProjectId, int ZoneId) : IRequest;

    public class UpdateProjectZoneCommandHandler : IRequestHandler<UpdateProjectZoneCommand>
    {
        private readonly IApplicationDbContext _context;

        public UpdateProjectZoneCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(UpdateProjectZoneCommand request, CancellationToken cancellationToken)
        {
            var project = await _context.Projects.FindAsync(new object[] { request.ProjectId }, cancellationToken);

            if (project == null)
            {
                // Should probably throw NotFoundException but for now simple return or exception
                throw new System.Exception("Project not found");
            }

            project.ZoneId = request.ZoneId;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
