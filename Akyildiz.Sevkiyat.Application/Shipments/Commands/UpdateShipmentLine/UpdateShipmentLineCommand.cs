using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.UpdateShipmentLine
{
    public class UpdateShipmentLineCommand : IRequest
    {
        public int ShipmentLineId { get; set; }
        public decimal NewQty { get; set; }
        public int? NewLocalStockId { get; set; } // If they changed the stock
    }

    public class UpdateShipmentLineCommandHandler : IRequestHandler<UpdateShipmentLineCommand>
    {
        private readonly IApplicationDbContext _context;

        public UpdateShipmentLineCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(UpdateShipmentLineCommand request, CancellationToken cancellationToken)
        {
            var line = await _context.ShipmentLines
                .Include(l => l.Shipment)
                .FirstOrDefaultAsync(l => l.Id == request.ShipmentLineId, cancellationToken);
            
            if (line == null) throw new NotFoundException("ShipmentLine", request.ShipmentLineId);

            // Allow updates only if Shipment is NOT yet Dispatched
            // Actually, Warehouse can edit during Preparation (Picking).
            // Once "ReadyForDispatch", it should be locked? 
            // Let's allow edit if status < ReadyForDispatch or if user is Admin.
            // For now, trust the validation in frontend + simple check.
            
            if (line.Shipment.Status == Domain.Enums.ShipmentStatus.Delivered || 
                line.Shipment.Status == Domain.Enums.ShipmentStatus.Cancelled)
            {
               throw new DomainException("Tamamlanmış veya iptal edilmiş sevkiyatta değişiklik yapılamaz.");
            }

            // Update Qty
            // Update Qty
            // line.OrderedQty = request.NewQty; // OLD: Updating OrderedQty
            
            // NEW: Update DeliveredQty
            line.SetDeliveredQty(request.NewQty, "Depo Düzenlemesi", "Micro Toplama Ekranından Güncellendi");
            
            string historyDesc = $"Stok Satırı Güncellendi: Teslim Miktarı {request.NewQty} olarak ayarlandı.";

            // Update Stock if requested
            if (request.NewLocalStockId.HasValue && request.NewLocalStockId.Value > 0)
            {
                var stock = await _context.StockMasters.FindAsync(new object[] { request.NewLocalStockId.Value }, cancellationToken);
                if (stock != null)
                {
                    historyDesc += $" Stok Kodu: {line.StockCode} -> {stock.StockCode}, Adı: {line.StockName} -> {stock.StockName} olarak değişti.";

                    line.UpdateStockInfo(stock.StockCode, stock.StockName, stock.Unit, stock.Id);
                }
            }

            // Log to History
            var history = new Domain.Entities.ShipmentHistory
            {
                ShipmentId = line.ShipmentId,
                OldStatus = line.Shipment.Status,
                NewStatus = line.Shipment.Status,
                ChangedAt = DateTime.UtcNow,
                ChangedByUserId = null, 
                Description = historyDesc
            };
            
            _context.ShipmentHistories.Add(history);

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
