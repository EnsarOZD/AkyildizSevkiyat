using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Application.PurchaseOrders.Commands.CreatePurchaseOrder
{
    public class CreatePurchaseOrderCommand : IRequest<Guid>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting", "Warehouse" };
        public Guid SupplierId { get; set; }
        public DateOnly OrderDate { get; set; }
        public DateOnly? ExpectedDeliveryDate { get; set; }
        public string? Note { get; set; }
        public string? ExternalRef { get; set; }

        public List<CreatePurchaseOrderLineDto> Lines { get; set; } = new();
    }

    public class CreatePurchaseOrderCommandHandler : IRequestHandler<CreatePurchaseOrderCommand, Guid>
    {
        private readonly IApplicationDbContext _context;

        public CreatePurchaseOrderCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(CreatePurchaseOrderCommand request, CancellationToken cancellationToken)
        {
            // 1. Validate Supplier
            var supplier = await _context.Suppliers.FindAsync(new object[] { request.SupplierId }, cancellationToken);
            if (supplier == null) throw new NotFoundException("Supplier", request.SupplierId);

            // 2. Validate Lines
            if (request.Lines == null || !request.Lines.Any())
            {
                throw new DomainException("At least one line is required.");
            }

            // 3. Generate Order Number (Transactionally Safe)
            string orderNumber = await GenerateOrderNumber(request.OrderDate, cancellationToken);

            // 4. Create Entity
            var entity = new PurchaseOrder
            {
                Id = Guid.NewGuid(),
                OrderNumber = orderNumber,
                SupplierId = request.SupplierId,
                SupplierNameSnapshot = supplier.Name,
                OrderDate = request.OrderDate,
                ExpectedDeliveryDate = request.ExpectedDeliveryDate,
                Note = request.Note,
                ExternalRef = request.ExternalRef,
                Status = PurchaseOrderStatus.Draft
            };

            // 5. Add Lines
            // Fetch Stocks to populate snapshots
            var stockIds = request.Lines.Select(x => x.StockMasterId).Distinct().ToList();
            var stocks = await _context.StockMasters
                .Where(x => stockIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellationToken);

            foreach (var lineDto in request.Lines)
            {
                if (!stocks.TryGetValue(lineDto.StockMasterId, out var stock))
                    throw new NotFoundException("Stock", lineDto.StockMasterId);

                entity.Lines.Add(new PurchaseOrderLine
                {
                    Id = Guid.NewGuid(),
                    PurchaseOrderId = entity.Id,
                    StockMasterId = lineDto.StockMasterId,
                    OrderedQty = lineDto.OrderedQty,
                    Unit = stock.Unit,
                    UnitPrice = lineDto.UnitPrice,
                    Note = lineDto.Note
                });
            }

            _context.PurchaseOrders.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            return entity.Id;
        }

        private async Task<string> GenerateOrderNumber(DateOnly date, CancellationToken cancellationToken)
        {
            int year = date.Year;
            int month = date.Month;
            
            var counter = await _context.PurchaseOrderNumberCounters
                .FirstOrDefaultAsync(x => x.Year == year && x.Month == month, cancellationToken);
            
            if (counter == null)
            {
                counter = new PurchaseOrderNumberCounter { Year = year, Month = month, LastValue = 0 };
                _context.PurchaseOrderNumberCounters.Add(counter);
            }
            
            counter.LastValue++;
            
            // Format: YYYYMM + 0000 + NNNNN (15 chars)
            string prefix = $"{year:0000}{month:00}"; 
            string sequence = counter.LastValue.ToString("D5"); 
            string fixedPadding = "0000";
            
            return $"{prefix}{fixedPadding}{sequence}";
        }
    }
}
