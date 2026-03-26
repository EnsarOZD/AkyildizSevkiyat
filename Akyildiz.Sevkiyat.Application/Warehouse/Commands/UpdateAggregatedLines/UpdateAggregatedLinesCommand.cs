using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.UpdateAggregatedLines
{
    public class UpdateAggregatedLinesCommand : IRequest<bool>
    {
        public int ZonePreparationId { get; set; }
        public List<int> ShipmentLineIds { get; set; } = new();
        public decimal NewTotalPickedQty { get; set; }
        public int? NewLocalStockId { get; set; }

        /// <summary>
        /// Toplam miktar sipariş miktarından farklıysa (az veya fazla) açıklama.
        /// Fazla toplama operasyonel olarak geçerlidir (koli tamamlama vb.).
        /// </summary>
        public string? DifferenceReason { get; set; }
    }

    public class UpdateAggregatedLinesCommandHandler : IRequestHandler<UpdateAggregatedLinesCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public UpdateAggregatedLinesCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<bool> Handle(UpdateAggregatedLinesCommand request, CancellationToken cancellationToken)
        {
            if (!request.ShipmentLineIds.Any()) return false;

            // 1. Validate Zone Prep
            var zp = await _context.ZonePreparations.FindAsync(new object[] { request.ZonePreparationId }, cancellationToken);
            if (zp == null) throw new NotFoundException("ZonePreparation", request.ZonePreparationId);

            if (!zp.IsFrozen)
                throw new DomainException("Hazırlık başlatılmadan miktar girişi yapılamaz.");

            if (zp.Status >= ZonePreparationStatus.ReadyForTransfer)
                throw new DomainException("Sevkiyat araca atandıktan sonra miktar değiştirilemez.");

            // 2. Fetch lines
            var lines = await _context.ShipmentLines
                .Include(sl => sl.Shipment)
                .Where(sl => request.ShipmentLineIds.Contains(sl.Id))
                .OrderBy(sl => sl.Id) // Stable order
                .ToListAsync(cancellationToken);

            if (lines.Any(l => l.Shipment.ZonePreparationId != request.ZonePreparationId))
                throw new DomainException("Bazı satırlar bu bölge hazırlığına ait değil.");

            // 3. Update Stock if requested
             Domain.Entities.StockMaster? newStock = null;
            if (request.NewLocalStockId.HasValue && request.NewLocalStockId.Value > 0)
            {
                 newStock = await _context.StockMasters.FindAsync(new object[] { request.NewLocalStockId.Value }, cancellationToken);
                 if (newStock != null)
                 {
                     foreach(var line in lines)
                     {
                         if (line.StockCode != newStock.StockCode)
                         {
                             line.UpdateStockInfo(newStock.StockCode, newStock.StockName, newStock.Unit, newStock.Id);
                         }
                     }
                 }
            }

            // 4. Distribute Quantity
            var totalOrdered = lines.Sum(l => l.OrderedQty);
            var qtyToDistribute = request.NewTotalPickedQty;

            if (qtyToDistribute < 0)
                throw new DomainException("Toplam toplama miktarı negatif olamaz.");

            // Fazla toplama (qtyToDistribute > totalOrdered) operasyonel olarak geçerlidir.
            // Örn: koli tamamlama — 10 sipariş, 12 toplama.
            // Bu durumda fazla miktar son satıra yüklenir (FIFO).
            // DifferenceReason zorunluluğu SetDeliveredQty içinde enforce edilir.

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                decimal qtyForThisLine = 0;
                
                // If it's the last line, give it everything remaining
                if (i == lines.Count - 1)
                {
                    qtyForThisLine = qtyToDistribute;
                }
                else
                {
                    // Regular distribution
                    if (qtyToDistribute >= line.OrderedQty)
                    {
                        qtyForThisLine = line.OrderedQty;
                        qtyToDistribute -= line.OrderedQty;
                    }
                    else
                    {
                        qtyForThisLine = qtyToDistribute;
                        qtyToDistribute = 0;
                    }
                }

                if (line.DeliveredQty != qtyForThisLine)
                {
                    var effectiveReason = qtyForThisLine != line.OrderedQty
                        ? (request.DifferenceReason ?? "Macro Dağıtım")
                        : null;

                    line.SetDeliveredQty(qtyForThisLine, effectiveReason, "Macro Toplama Otomatik Dağıtım");

                    _context.ShipmentHistories.Add(new ShipmentHistory {
                        ShipmentId      = line.ShipmentId,
                        OldStatus       = line.Shipment.Status,
                        NewStatus       = line.Shipment.Status,
                        ChangedAt       = DateTime.UtcNow,
                        ChangedByUserId = _currentUserService.UserId,
                        Description     = $"Macro Dağıtım: {qtyForThisLine} adet " +
                                          $"(Toplam girilen: {request.NewTotalPickedQty}, Sipariş: {totalOrdered})"
                    });
                }
            }

            // If there is remainder (qtyToDistribute > 0), it means we clamped it earlier, so it's fine. 
            // We just filled completely.

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
