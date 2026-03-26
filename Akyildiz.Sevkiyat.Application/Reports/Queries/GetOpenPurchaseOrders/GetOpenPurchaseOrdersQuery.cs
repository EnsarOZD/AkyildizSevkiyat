using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Reports.Queries.GetOpenPurchaseOrders
{
    public record GetOpenPurchaseOrdersQuery : IRequest<List<OpenPurchaseOrderRow>>;

    public class OpenPurchaseOrderRow
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public DateOnly OrderDate { get; set; }
        public DateOnly? ExpectedDeliveryDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int LineCount { get; set; }
    }

    public class GetOpenPurchaseOrdersQueryHandler : IRequestHandler<GetOpenPurchaseOrdersQuery, List<OpenPurchaseOrderRow>>
    {
        private readonly IApplicationDbContext _context;

        public GetOpenPurchaseOrdersQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<OpenPurchaseOrderRow>> Handle(GetOpenPurchaseOrdersQuery request, CancellationToken cancellationToken)
        {
            var openStatuses = new[] { PurchaseOrderStatus.Approved, PurchaseOrderStatus.PartiallyReceived };

            return await _context.PurchaseOrders
                .Include(po => po.Lines)
                .Where(po => openStatuses.Contains(po.Status))
                .OrderBy(po => po.ExpectedDeliveryDate)
                .ThenBy(po => po.OrderDate)
                .Select(po => new OpenPurchaseOrderRow
                {
                    Id                   = po.Id,
                    OrderNumber          = po.OrderNumber,
                    SupplierName         = po.SupplierNameSnapshot,
                    OrderDate            = po.OrderDate,
                    ExpectedDeliveryDate = po.ExpectedDeliveryDate,
                    Status               = po.Status.ToString(),
                    LineCount            = po.Lines.Count
                })
                .ToListAsync(cancellationToken);
        }
    }
}
