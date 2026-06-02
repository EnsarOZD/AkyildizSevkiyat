using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.InstitutionCariMappings.Commands.UpdateInstitutionCariMapping
{
    public record UpdateInstitutionCariMappingCommand(
        int Id,
        string InstitutionCode,
        string NetsisCariKodu,
        string? Description,
        bool IsActive
    ) : IRequest, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager", "Accounting" };
    }

    public class UpdateInstitutionCariMappingCommandHandler
        : IRequestHandler<UpdateInstitutionCariMappingCommand>
    {
        private readonly IApplicationDbContext _context;

        public UpdateInstitutionCariMappingCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(UpdateInstitutionCariMappingCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.InstitutionCariMappings
                .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);
            if (entity is null)
                throw new NotFoundException($"Eşleşme #{request.Id} bulunamadı.");

            var kurum = request.InstitutionCode.Trim();
            var cari = request.NetsisCariKodu.Trim();

            if (string.IsNullOrWhiteSpace(kurum))
                throw new DomainException("Kurum kodu boş olamaz.");
            if (string.IsNullOrWhiteSpace(cari))
                throw new DomainException("Netsis Cari Kodu boş olamaz.");

            var duplicate = await _context.InstitutionCariMappings
                .AnyAsync(m => m.Id != request.Id && m.InstitutionCode == kurum, cancellationToken);
            if (duplicate)
                throw new ConflictException($"'{kurum}' kurum kodu başka bir kayıt için zaten tanımlı.");

            entity.InstitutionCode = kurum;
            entity.NetsisCariKodu = cari;
            entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            entity.IsActive = request.IsActive;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
