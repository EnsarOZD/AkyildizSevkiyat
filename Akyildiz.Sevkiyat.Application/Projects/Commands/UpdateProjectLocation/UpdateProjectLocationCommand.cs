using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Projects.Commands.UpdateProjectLocation
{
    public record UpdateProjectLocationCommand(int ProjectId, double? Latitude, double? Longitude) : IRequest;

    public class UpdateProjectLocationCommandHandler : IRequestHandler<UpdateProjectLocationCommand>
    {
        private readonly IApplicationDbContext _context;

        public UpdateProjectLocationCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(UpdateProjectLocationCommand request, CancellationToken cancellationToken)
        {
            var project = await _context.Projects.FindAsync(new object[] { request.ProjectId }, cancellationToken)
                ?? throw new NotFoundException("Project", request.ProjectId);

            project.Latitude = request.Latitude;
            project.Longitude = request.Longitude;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
