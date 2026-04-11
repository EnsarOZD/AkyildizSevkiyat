using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Projects.Commands.UpdateProjectNetsisCariKodu
{
    public record UpdateProjectNetsisCariKoduCommand(int Id, string? NetsisCariKodu, string? NetsisTeslimCariKodu = null) : IRequest;

    public class UpdateProjectNetsisCariKoduCommandHandler : IRequestHandler<UpdateProjectNetsisCariKoduCommand>
    {
        private readonly IApplicationDbContext _context;

        public UpdateProjectNetsisCariKoduCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(UpdateProjectNetsisCariKoduCommand request, CancellationToken cancellationToken)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (project == null)
                throw new NotFoundException("Project", request.Id);

            project.NetsisCariKodu = string.IsNullOrWhiteSpace(request.NetsisCariKodu)
                ? null
                : request.NetsisCariKodu.Trim();

            if (request.NetsisTeslimCariKodu is not null)
                project.NetsisTeslimCariKodu = string.IsNullOrWhiteSpace(request.NetsisTeslimCariKodu)
                    ? null
                    : request.NetsisTeslimCariKodu.Trim();

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
