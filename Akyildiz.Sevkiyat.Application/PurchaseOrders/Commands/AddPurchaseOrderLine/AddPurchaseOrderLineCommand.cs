using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Application.PurchaseOrders.Commands.AddPurchaseOrderLine
{
    public class AddPurchaseOrderLineCommand : IRequest<Guid>
    {
        public Guid PurchaseOrderId { get; set; }
        public int StockMasterId { get; set; }
        public decimal OrderedQty { get; set; }
        public StockUnit Unit { get; set; }
        public string? Note { get; set; }
    }

    public class AddPurchaseOrderLineCommandHandler : IRequestHandler<AddPurchaseOrderLineCommand, Guid>
    {
        private readonly IApplicationDbContext _context;

        public AddPurchaseOrderLineCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(AddPurchaseOrderLineCommand request, CancellationToken cancellationToken)
        {
            var po = await _context.PurchaseOrders
                .FirstOrDefaultAsync(x => x.Id == request.PurchaseOrderId, cancellationToken);
            
            if (po == null) throw new NotFoundException("PurchaseOrder", request.PurchaseOrderId);
            if (!po.IsEditable) throw new DomainException("Purchase Order is not editable");

            var stock = await _context.StockMasters.FindAsync(new object[] { request.StockMasterId }, cancellationToken);
            if (stock == null) throw new NotFoundException("Stock", request.StockMasterId);

            var line = new PurchaseOrderLine
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = request.PurchaseOrderId,
                StockMasterId = request.StockMasterId,
                OrderedQty = request.OrderedQty,
                
                // Server-side Snapshot
                Unit = stock.Unit,
                
                Note = request.Note
            };

            _context.PurchaseOrderLines.Add(line);
            await _context.SaveChangesAsync(cancellationToken);

            return line.Id;
        }
    }
}
