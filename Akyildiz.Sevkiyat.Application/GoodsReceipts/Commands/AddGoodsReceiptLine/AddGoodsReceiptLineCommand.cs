using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Application.GoodsReceipts.Commands.AddGoodsReceiptLine
{
    public class AddGoodsReceiptLineCommand : IRequest<Guid>
    {
        public Guid GoodsReceiptId { get; set; }
        
        public Guid? PurchaseOrderLineId { get; set; }
        public int StockMasterId { get; set; }
        
        public decimal ReceivedQty { get; set; }
        public decimal? RejectedQty { get; set; }
        public string? RejectReason { get; set; }
        
        public string? Note { get; set; }
    }

    public class AddGoodsReceiptLineCommandHandler : IRequestHandler<AddGoodsReceiptLineCommand, Guid>
    {
        private readonly IApplicationDbContext _context;

        public AddGoodsReceiptLineCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(AddGoodsReceiptLineCommand request, CancellationToken cancellationToken)
        {
            var gr = await _context.GoodsReceipts
                .FirstOrDefaultAsync(x => x.Id == request.GoodsReceiptId, cancellationToken);
            
            if (gr == null) throw new NotFoundException("GoodsReceipt", request.GoodsReceiptId);
            
            // Immutability Check
            if (gr.Status != GoodsReceiptStatus.Draft) 
            {
                throw new DomainException("Cannot add lines to a non-Draft Goods Receipt.");
            }

            var stock = await _context.StockMasters.FindAsync(new object[] { request.StockMasterId }, cancellationToken);
            if (stock == null) throw new NotFoundException("Stock", request.StockMasterId);

            // If PO Line ID is provided, validate it? 
            // Optional: check if PO Line belongs to GR's PO.
            if (request.PurchaseOrderLineId.HasValue && gr.PurchaseOrderId.HasValue)
            {
               var poLine = await _context.PurchaseOrderLines
                   .FirstOrDefaultAsync(x => x.Id == request.PurchaseOrderLineId.Value, cancellationToken);
               
               if (poLine != null && poLine.PurchaseOrderId != gr.PurchaseOrderId)
               {
                   throw new DomainException("Purchase Order Line does not belong to the Purchase Order linked to this Receipt.");
               }
            }

            if (request.RejectedQty.HasValue && request.RejectedQty.Value > request.ReceivedQty)
            {
                throw new DomainException("Rejected quantity cannot exceed received quantity.");
            }

            if (request.RejectedQty.HasValue && request.RejectedQty.Value > 0 && string.IsNullOrWhiteSpace(request.RejectReason))
            {
                throw new DomainException("Reject reason is required when there is a rejection.");
            }

            var line = new GoodsReceiptLine
            {
                Id = Guid.NewGuid(),
                GoodsReceiptId = request.GoodsReceiptId,
                PurchaseOrderLineId = request.PurchaseOrderLineId,
                StockMasterId = request.StockMasterId,
                
                // Server-side Snapshots
                StockNameSnapshot = stock.StockName,
                UnitSnapshot = stock.Unit, 
                
                OrderedQty = 0, // Default for manual entries; if PO line linked, we should snapshot it
                ReceivedQty = request.ReceivedQty,
                RejectedQty = request.RejectedQty ?? 0,
                AcceptedQty = request.ReceivedQty - (request.RejectedQty ?? 0),
                RejectReason = request.RejectReason,
                Note = request.Note
            };
            
            // If linked to PO, snapshot OrderedQty
            if (request.PurchaseOrderLineId.HasValue)
            {
                var poLine = await _context.PurchaseOrderLines.FindAsync(new object[] { request.PurchaseOrderLineId.Value }, cancellationToken);
                if (poLine != null)
                {
                    line.OrderedQty = poLine.OrderedQty;
                }
            }

            _context.GoodsReceiptLines.Add(line);
            await _context.SaveChangesAsync(cancellationToken);

            return line.Id;
        }
    }
}
