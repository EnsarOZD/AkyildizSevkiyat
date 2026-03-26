using Akyildiz.Sevkiyat.Application.Common.Models;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.PurchaseOrders.Queries.GetPurchaseOrders
{
    public class GetPurchaseOrdersQuery : IRequest<PaginatedList<PurchaseOrderDto>>
    {
        // Filters
        public PurchaseOrderStatus? Status { get; set; }
        public string? SupplierName { get; set; }
        public DateOnly? StartData { get; set; }
        public DateOnly? EndDate { get; set; }
        // Pagination
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class PurchaseOrderDto
    {
        public Guid Id { get; set; }
        public Guid SupplierId { get; set; }
        public string SupplierNameSnapshot { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public DateOnly OrderDate { get; set; }
        public DateOnly? ExpectedDeliveryDate { get; set; }
        public string Status { get; set; } = string.Empty; 
        public int StatusValue { get; set; }
        public string? Note { get; set; }
        public int LineCount { get; set; }
    }

    public class GetPurchaseOrdersQueryHandler : IRequestHandler<GetPurchaseOrdersQuery, PaginatedList<PurchaseOrderDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetPurchaseOrdersQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedList<PurchaseOrderDto>> Handle(GetPurchaseOrdersQuery request, CancellationToken cancellationToken)
        {
            var query = _context.PurchaseOrders.AsQueryable();

            if (request.Status.HasValue)
                query = query.Where(x => x.Status == request.Status.Value);

            if (!string.IsNullOrEmpty(request.SupplierName))
                query = query.Where(x => EF.Functions.Collate(x.SupplierNameSnapshot, "Turkish_CI_AS").Contains(EF.Functions.Collate(request.SupplierName, "Turkish_CI_AS")));

            if (request.StartData.HasValue)
                query = query.Where(x => x.OrderDate >= request.StartData.Value);

            if (request.EndDate.HasValue)
                query = query.Where(x => x.OrderDate <= request.EndDate.Value);

            var projected = query
                .OrderByDescending(x => x.OrderDate)
                .Select(x => new PurchaseOrderDto
                {
                    Id = x.Id,
                    SupplierId = x.SupplierId,
                    SupplierNameSnapshot = x.SupplierNameSnapshot,
                    OrderNumber = x.OrderNumber,
                    OrderDate = x.OrderDate,
                    ExpectedDeliveryDate = x.ExpectedDeliveryDate,
                    Status = x.Status.ToString(),
                    StatusValue = (int)x.Status,
                    Note = x.Note,
                    LineCount = x.Lines.Count
                });

            return await PaginatedList<PurchaseOrderDto>.CreateAsync(projected, request.PageNumber, request.PageSize);
        }
    }
}
