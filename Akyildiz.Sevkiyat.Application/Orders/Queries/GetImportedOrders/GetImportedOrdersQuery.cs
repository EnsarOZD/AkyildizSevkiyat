using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.Common.Models;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Orders.Queries.GetImportedOrders
{
    public record ImportedOrderDto(
        int Id,
        string ExternalOrderNumber,
        DateTime OrderDate,
        DateTime DeliveryDate,
        string Status,
        string ImportStatus,
        int LineCount,
        bool NeedsMapping,
        string ProjectCode,
        string ProjectName,
        string InstitutionCode,
        string Region,
        // New Fields
        string? TalepNo,
        string? TalepTuru,
        string? Aciklama,
        string? TeslimAlacakKisiler,
        string? TeslimAlacakTelefonNumaralari,
        string? YoneticiMailAdresleri,
        bool IsActive
    );

    public class GetImportedOrdersQuery : IRequest<PaginatedList<ImportedOrderDto>>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Driver", "Accounting" };

        public string Tab { get; set; } = "Ready";
        public string? Search { get; set; }
        public string? Zone { get; set; }
        public string? TalepNoStatus { get; set; } // "All", "Zero", "NonZero"
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class GetImportedOrdersQueryHandler : IRequestHandler<GetImportedOrdersQuery, PaginatedList<ImportedOrderDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetImportedOrdersQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedList<ImportedOrderDto>> Handle(GetImportedOrdersQuery request, CancellationToken cancellationToken)
        {
            var query = _context.IssOrders
                .Include(o => o.Project)
                .ThenInclude(p => p.Zone)
                .AsQueryable();

            if (request.Tab == "Passive")
            {
                query = query.Where(o => !o.IsActive
                    && !_context.Shipments.Any(s => s.IssOrderId == o.Id && s.Status != ShipmentStatus.Cancelled));
            }
            else
            {
                // Exclude orders already transferred to a shipment, or with any active (non-cancelled) shipment
                query = query.Where(o => o.IsActive
                    && !o.IsTransferred
                    && !_context.Shipments.Any(s => s.IssOrderId == o.Id && s.Status != ShipmentStatus.Cancelled));

                if (request.Tab == "NeedsMapping")
                {
                    query = query.Where(o => o.ImportStatus == ImportStatus.NeedsMapping);
                }
                else // "Ready"
                {
                    query = query.Where(o => o.ImportStatus == ImportStatus.Ready);
                }
            }

            // Search logic remains same ...
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                string s = request.Search.Trim();
                query = query.Where(o => 
                    EF.Functions.Collate(o.ExternalOrderNumber, "Turkish_CI_AS").Contains(EF.Functions.Collate(s, "Turkish_CI_AS")) || 
                    (o.TalepNo != null && EF.Functions.Collate(o.TalepNo, "Turkish_CI_AS").Contains(EF.Functions.Collate(s, "Turkish_CI_AS"))) ||
                    EF.Functions.Collate(o.Project.Name, "Turkish_CI_AS").Contains(EF.Functions.Collate(s, "Turkish_CI_AS")) ||
                    EF.Functions.Collate(o.Project.Code, "Turkish_CI_AS").Contains(EF.Functions.Collate(s, "Turkish_CI_AS")) ||
                    (o.Project.InstitutionCode != null && EF.Functions.Collate(o.Project.InstitutionCode, "Turkish_CI_AS").Contains(EF.Functions.Collate(s, "Turkish_CI_AS")))
                );
            }

            if (!string.IsNullOrWhiteSpace(request.Zone))
            {
                string z = request.Zone.Trim();
                query = query.Where(o =>
                    (o.Project != null && o.Project.Zone != null && EF.Functions.Collate(o.Project.Zone.Name, "Turkish_CI_AS").Contains(EF.Functions.Collate(z, "Turkish_CI_AS"))) ||
                    (o.Project != null && o.Project.Zone == null && o.Project.Region != null && EF.Functions.Collate(o.Project.Region, "Turkish_CI_AS").Contains(EF.Functions.Collate(z, "Turkish_CI_AS")))
                );
            }

            if (!string.IsNullOrWhiteSpace(request.TalepNoStatus))
            {
                if (request.TalepNoStatus == "Zero")
                {
                    query = query.Where(o => o.TalepNo == "0");
                }
                else if (request.TalepNoStatus == "NonZero")
                {
                    query = query.Where(o => o.TalepNo == null || o.TalepNo != "0");
                }
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(o => new ImportedOrderDto(
                    o.Id,
                    o.ExternalOrderNumber,
                    o.OrderDate,
                    o.DeliveryDate,
                    o.Status.ToString(),
                    o.ImportStatus.ToString(),
                    o.Lines.Count,
                    o.ImportStatus == ImportStatus.NeedsMapping,
                    o.Project != null ? o.Project.Code : "",
                    o.Project != null ? o.Project.Name : "",
                    o.Project != null ? (o.Project.InstitutionCode ?? "") : "",
                    o.Project != null ? (o.Project.Zone != null ? o.Project.Zone.Name : o.Project.Region ?? "") : "",
                    o.TalepNo,
                    o.TalepTuru,
                    o.Aciklama,
                    o.TeslimAlacakKisiler,
                    o.TeslimAlacakTelefonNumaralari,
                    o.YoneticiMailAdresleri,
                    o.IsActive
                ))
                .ToListAsync(cancellationToken);
                
            return new PaginatedList<ImportedOrderDto>(items, totalCount, request.PageNumber, request.PageSize);
        }
    }
}
