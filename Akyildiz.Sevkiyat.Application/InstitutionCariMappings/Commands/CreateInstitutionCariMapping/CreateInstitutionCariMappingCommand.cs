using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.InstitutionCariMappings.Commands.CreateInstitutionCariMapping
{
    public record CreateInstitutionCariMappingCommand(
        string InstitutionCode,
        string NetsisCariKodu,
        string? Description = null
    ) : IRequest<int>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager", "Accounting" };
    }

    public class CreateInstitutionCariMappingCommandHandler
        : IRequestHandler<CreateInstitutionCariMappingCommand, int>
    {
        private readonly IApplicationDbContext _context;

        public CreateInstitutionCariMappingCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(CreateInstitutionCariMappingCommand request, CancellationToken cancellationToken)
        {
            var kurum = request.InstitutionCode.Trim();
            var cari = request.NetsisCariKodu.Trim();

            if (string.IsNullOrWhiteSpace(kurum))
                throw new DomainException("Kurum kodu boş olamaz.");
            if (string.IsNullOrWhiteSpace(cari))
                throw new DomainException("Netsis Cari Kodu boş olamaz.");

            var exists = await _context.InstitutionCariMappings
                .AnyAsync(m => m.InstitutionCode == kurum, cancellationToken);
            if (exists)
                throw new ConflictException($"'{kurum}' kurum kodu için eşleşme zaten tanımlı.");

            var entity = new InstitutionCariMapping
            {
                InstitutionCode = kurum,
                NetsisCariKodu = cari,
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                IsActive = true,
            };

            _context.InstitutionCariMappings.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return entity.Id;
        }
    }
}
