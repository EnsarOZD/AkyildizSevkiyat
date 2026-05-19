using MediatR;
using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Common.Models;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Shipments.Queries.GetShipments
{
    public class ShipmentDto
    {
        public int Id { get; set; }
        public string ProjectCode { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime DeliveryDate { get; set; }
        public string? DriverName { get; set; }
        public string? PlateNumber { get; set; }
        // New Fields
        public string? TalepNo { get; set; }
        public string? TalepTuru { get; set; }
        public string? InstitutionCode { get; set; }
        public string? ExternalOrderNumber { get; set; }
        public string? WaybillNumber { get; set; }
        public string? Aciklama { get; set; }
        public DateTime? NetsisTransferredAt { get; set; }
        public string OperationType { get; set; } = "Catering";
        public int OperationTypeValue { get; set; } = 0;
        // Dispatch type fields
        public int? CargoProviderValue { get; set; }
        public string? FreightCarrierName { get; set; }
        public string? FreightCarrierPlate { get; set; }
    }

    public class GetShipmentsQuery : IRequest<PaginatedList<ShipmentDto>>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Warehouse", "Driver", "Accounting" };

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public ShipmentStatus? Status { get; set; }
        /// <summary>Virgülle ayrılmış status değerleri. Örn: "5,6" → Dispatched + Delivered.</summary>
        public string? Statuses { get; set; }
        public string? Region { get; set; }
        public bool IncludePassive { get; set; } = false;

        // Search & Pagination
        public string? Search { get; set; }
        public int? ZoneId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        // "Cargo" | "Freight" | "Vehicle" | "None"
        public string? DispatchType { get; set; }
        // 0 = Catering, 1 = Clothing
        public int? OperationType { get; set; }
    }
    
    public class GetShipmentsQueryHandler : IRequestHandler<GetShipmentsQuery, PaginatedList<ShipmentDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetShipmentsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedList<ShipmentDto>> Handle(GetShipmentsQuery request, CancellationToken cancellationToken)
        {
             var query = _context.Shipments
                .Include(s => s.Project)
                .ThenInclude(p => p.Zone)
                .Include(s => s.IssOrder) // Include Order for TalepNo/Aciklama
                .AsQueryable();

            if (request.StartDate.HasValue)
            {
                query = query.Where(s => s.DeliveryDate >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                query = query.Where(s => s.DeliveryDate <= request.EndDate.Value);
            }

            // Çoklu status filtresi (virgülle ayrılmış): tekil Status'tan öncelikli
            var parsedStatuses = string.IsNullOrWhiteSpace(request.Statuses)
                ? null
                : request.Statuses.Split(',', System.StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.TryParse(s.Trim(), out var v) ? (int?)v : null)
                    .Where(v => v.HasValue)
                    .Select(v => (Akyildiz.Sevkiyat.Domain.Enums.ShipmentStatus)v!.Value)
                    .ToList();

            if (parsedStatuses != null && parsedStatuses.Count > 0)
            {
                query = query.Where(s => parsedStatuses.Contains(s.Status));
            }
            else if (request.Status.HasValue)
            {
                query = query.Where(s => s.Status == request.Status.Value);
            }
            else if (!request.IncludePassive)
            {
                query = query.Where(s => s.Status != Akyildiz.Sevkiyat.Domain.Enums.ShipmentStatus.Passive);
            }

            if (!string.IsNullOrEmpty(request.Region))
            {
                query = query.Where(s => s.Project.Region == request.Region || (s.Project.Zone != null && s.Project.Zone.Name == request.Region));
            }

            if (request.ZoneId.HasValue)
            {
                query = query.Where(s => s.Project.ZoneId == request.ZoneId.Value);
            }

            if (!string.IsNullOrEmpty(request.DispatchType))
            {
                query = request.DispatchType switch
                {
                    "Cargo"   => query.Where(s => s.CargoProvider != null),
                    "Freight" => query.Where(s => s.FreightCarrierName != null && s.CargoProvider == null),
                    "Vehicle" => query.Where(s => s.AssignedDriverName != null && s.CargoProvider == null && s.FreightCarrierName == null),
                    "None"    => query.Where(s => s.CargoProvider == null && s.FreightCarrierName == null && s.AssignedDriverName == null),
                    _         => query
                };
            }

            if (request.OperationType.HasValue)
            {
                var opType = (Domain.Enums.OperationType)request.OperationType.Value;
                query = query.Where(s => s.OperationType == opType);
            }

            // Search
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                string s = request.Search.Trim();
                // Simple integer check for ID if it parses
                if (int.TryParse(s, out int id))
                {
                    query = query.Where(x =>
                        x.Id == id ||
                        EF.Functions.Collate(x.Project.Name, "Turkish_CI_AS").Contains(EF.Functions.Collate(s, "Turkish_CI_AS")) ||
                        EF.Functions.Collate(x.Project.Code, "Turkish_CI_AS").Contains(EF.Functions.Collate(s, "Turkish_CI_AS")) ||
                        (x.IssOrder != null && EF.Functions.Collate(x.IssOrder.ExternalOrderNumber, "Turkish_CI_AS").Contains(EF.Functions.Collate(s, "Turkish_CI_AS"))) ||
                        (x.IssOrder != null && x.IssOrder.TalepNo != null && EF.Functions.Collate(x.IssOrder.TalepNo, "Turkish_CI_AS").Contains(EF.Functions.Collate(s, "Turkish_CI_AS"))));
                }
                else
                {
                    query = query.Where(x => 
                        EF.Functions.Collate(x.Project.Name, "Turkish_CI_AS").Contains(EF.Functions.Collate(s, "Turkish_CI_AS")) || 
                        EF.Functions.Collate(x.Project.Code, "Turkish_CI_AS").Contains(EF.Functions.Collate(s, "Turkish_CI_AS")) ||
                        (x.IssOrder.TalepNo != null && EF.Functions.Collate(x.IssOrder.TalepNo, "Turkish_CI_AS").Contains(EF.Functions.Collate(s, "Turkish_CI_AS"))) ||
                        (x.IssOrder.ExternalOrderNumber != null && EF.Functions.Collate(x.IssOrder.ExternalOrderNumber, "Turkish_CI_AS").Contains(EF.Functions.Collate(s, "Turkish_CI_AS"))) ||
                        (x.AssignedDriverName != null && EF.Functions.Collate(x.AssignedDriverName, "Turkish_CI_AS").Contains(EF.Functions.Collate(s, "Turkish_CI_AS")))
                    );
                }
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(s => s.DeliveryDate.Date)
                .ThenBy(s => s.Project.Name)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(s => new ShipmentDto
                {
                    Id = s.Id,
                    ProjectCode = s.Project.Code,
                    ProjectName = s.Project.Name,
                    Region = s.Project.Zone != null ? s.Project.Zone.Name : s.Project.Region ?? "",
                    Status = s.Status.ToString(),
                    DeliveryDate = s.DeliveryDate,
                    DriverName = s.AssignedDriverName,
                    PlateNumber = s.AssignedPlateNumber,
                    TalepNo = s.IssOrder != null ? s.IssOrder.TalepNo : null,
                    TalepTuru = s.IssOrder != null ? s.IssOrder.TalepTuru : null,
                    InstitutionCode = s.Project.InstitutionCode,
                    ExternalOrderNumber = s.IssOrder != null ? s.IssOrder.ExternalOrderNumber : null,
                    WaybillNumber = s.IrsaliyeNo,
                    Aciklama = s.IssOrder != null ? s.IssOrder.Aciklama : null,
                    NetsisTransferredAt  = s.NetsisTransferredAt,
                    OperationType        = s.OperationType == Domain.Enums.OperationType.Clothing ? "Kıyafet" : "Catering",
                    OperationTypeValue   = (int)s.OperationType,
                    CargoProviderValue   = s.CargoProvider != null ? (int?)s.CargoProvider.Value : null,
                    FreightCarrierName   = s.FreightCarrierName,
                    FreightCarrierPlate  = s.FreightCarrierPlate,
                }).ToListAsync(cancellationToken);

            return new PaginatedList<ShipmentDto>(items, totalCount, request.PageNumber, request.PageSize);
        }
    }
}
