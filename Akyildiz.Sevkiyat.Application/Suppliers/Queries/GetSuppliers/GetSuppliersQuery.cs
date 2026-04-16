using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Suppliers.Queries.GetSuppliers
{
    public class GetSuppliersQuery : IRequest<List<SupplierDto>>
    {
        public string? Search { get; set; }
    }

    public class SupplierDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? SupplierCode { get; set; }
        public string? Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastModified { get; set; }
    }

    public class GetSuppliersQueryHandler : IRequestHandler<GetSuppliersQuery, List<SupplierDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetSuppliersQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<SupplierDto>> Handle(GetSuppliersQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Suppliers.AsQueryable();

            if (!string.IsNullOrEmpty(request.Search))
            {
                var searchTerm = request.Search.Trim();
                query = query.Where(x => 
                    EF.Functions.Collate(x.Name, "Turkish_CI_AS").Contains(EF.Functions.Collate(searchTerm, "Turkish_CI_AS")) || 
                    (x.SupplierCode != null && EF.Functions.Collate(x.SupplierCode, "Turkish_CI_AS").Contains(EF.Functions.Collate(searchTerm, "Turkish_CI_AS"))));
            }

            return await query
                .OrderBy(x => x.Name)
                .Select(x => new SupplierDto { Id = x.Id, Name = x.Name, SupplierCode = x.SupplierCode, Email = x.Email, CreatedAt = x.CreatedAt, LastModified = x.LastModified })
                .ToListAsync(cancellationToken);
        }
    }
}
