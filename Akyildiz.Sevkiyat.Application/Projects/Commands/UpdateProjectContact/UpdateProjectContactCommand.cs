using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Projects.Commands.UpdateProjectContact
{
    public record UpdateProjectContactCommand(int ProjectId, string? ContactName, string? ContactPhone) : IRequest;

    public class UpdateProjectContactCommandHandler : IRequestHandler<UpdateProjectContactCommand>
    {
        private readonly IApplicationDbContext _context;

        public UpdateProjectContactCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(UpdateProjectContactCommand request, CancellationToken cancellationToken)
        {
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
                ?? throw new NotFoundException("Proje bulunamadı.");

            project.DefaultContactName  = string.IsNullOrWhiteSpace(request.ContactName)  ? null : request.ContactName.Trim();
            project.DefaultContactPhone = string.IsNullOrWhiteSpace(request.ContactPhone) ? null : request.ContactPhone.Trim();

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
