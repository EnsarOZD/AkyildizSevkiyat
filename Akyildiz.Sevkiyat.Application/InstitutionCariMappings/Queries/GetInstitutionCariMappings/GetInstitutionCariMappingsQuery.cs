using Akyildiz.Sevkiyat.Application.Common.Dtos;
using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.InstitutionCariMappings.Queries.GetInstitutionCariMappings
{
    public record GetInstitutionCariMappingsQuery(bool ShowInactive = false)
        : IRequest<List<InstitutionCariMappingDto>>;

    public class GetInstitutionCariMappingsQueryHandler
        : IRequestHandler<GetInstitutionCariMappingsQuery, List<InstitutionCariMappingDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetInstitutionCariMappingsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<InstitutionCariMappingDto>> Handle(
            GetInstitutionCariMappingsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.InstitutionCariMappings.AsQueryable();
            if (!request.ShowInactive)
                query = query.Where(m => m.IsActive);

            return await query
                .OrderBy(m => m.InstitutionCode)
                .Select(m => new InstitutionCariMappingDto(
                    m.Id, m.InstitutionCode, m.NetsisCariKodu, m.Description, m.IsActive))
                .ToListAsync(cancellationToken);
        }
    }
}
