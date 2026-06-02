using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.InstitutionCariMappings.Commands.DeleteInstitutionCariMapping
{
    public record DeleteInstitutionCariMappingCommand(int Id)
        : IRequest, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager", "Accounting" };
    }

    public class DeleteInstitutionCariMappingCommandHandler
        : IRequestHandler<DeleteInstitutionCariMappingCommand>
    {
        private readonly IApplicationDbContext _context;

        public DeleteInstitutionCariMappingCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(DeleteInstitutionCariMappingCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.InstitutionCariMappings
                .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);
            if (entity is null)
                throw new NotFoundException($"Eşleşme #{request.Id} bulunamadı.");

            _context.InstitutionCariMappings.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
