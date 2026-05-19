using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.ExternalEmailContacts.Queries
{
    public record ExternalEmailContactDto(int Id, string Name, string Email, string? Note, bool IsActive);

    public record GetExternalEmailContactsQuery(bool? ActiveOnly = null) : IRequest<List<ExternalEmailContactDto>>;

    public class GetExternalEmailContactsQueryHandler : IRequestHandler<GetExternalEmailContactsQuery, List<ExternalEmailContactDto>>
    {
        private readonly IApplicationDbContext _context;
        public GetExternalEmailContactsQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<List<ExternalEmailContactDto>> Handle(GetExternalEmailContactsQuery request, CancellationToken cancellationToken)
        {
            var q = _context.ExternalEmailContacts.AsQueryable();
            if (request.ActiveOnly == true)
                q = q.Where(x => x.IsActive);
            return await q
                .OrderBy(x => x.Name)
                .Select(x => new ExternalEmailContactDto(x.Id, x.Name, x.Email, x.Note, x.IsActive))
                .ToListAsync(cancellationToken);
        }
    }
}
