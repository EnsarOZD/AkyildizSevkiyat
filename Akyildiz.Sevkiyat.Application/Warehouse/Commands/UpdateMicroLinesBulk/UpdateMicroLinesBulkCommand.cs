using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.UpdateMicroLinesBulk
{
    public class MicroLineUpdateDto
    {
        public int ShipmentLineId { get; set; }
        public decimal DeliveredQty { get; set; }
        public int? NewLocalStockId { get; set; }

        /// <summary>
        /// Toplanan miktar sipariş miktarından farklıysa (az veya fazla) açıklama.
        /// Fazla toplama örneği: "Koli tamamlama — 12 yerine 20 gönderildi."
        /// </summary>
        public string? DifferenceReason { get; set; }
    }

    public class UpdateMicroLinesBulkCommand : IRequest<bool>
    {
        public int ZonePreparationProjectId { get; set; }
        public List<MicroLineUpdateDto> Lines { get; set; } = new();
    }

    public class UpdateMicroLinesBulkCommandHandler : IRequestHandler<UpdateMicroLinesBulkCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public UpdateMicroLinesBulkCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<bool> Handle(UpdateMicroLinesBulkCommand request, CancellationToken cancellationToken)
        {
            if (request.Lines == null || !request.Lines.Any()) return true;

            // 1. Verify Project
            var zpProject = await _context.ZonePreparationProjects
                .Include(p => p.ZonePreparation)
                .FirstOrDefaultAsync(p => p.Id == request.ZonePreparationProjectId, cancellationToken);
            
            if (zpProject == null) throw new NotFoundException("ZonePreparationProject", request.ZonePreparationProjectId);

            // Guard: Ensure Zone isn't already closed/transferred
            if (zpProject.ZonePreparation.Status >= ZonePreparationStatus.ReadyForTransfer)
                throw new DomainException("Sevkiyat araca atandıktan sonra miktar değiştirilemez.");
            
            // Guard: Ensure started
            if (!zpProject.ZonePreparation.IsFrozen)
                throw new DomainException("Hazırlık başlatılmadan miktar girişi yapılamaz.");

            // 2. Sahiplik doğrulaması — gelen ID'ler bu proje + zone'a ait olmalı
            var lineIds = request.Lines.Select(l => l.ShipmentLineId).ToList();

            var validLineIds = await _context.ShipmentLines
                .Where(l => l.Shipment.ZonePreparationId == zpProject.ZonePreparationId
                         && l.Shipment.ProjectId == zpProject.ProjectId)
                .Select(l => l.Id)
                .ToListAsync(cancellationToken);

            var validSet = validLineIds.ToHashSet();
            var invalidIds = lineIds.Where(id => !validSet.Contains(id)).ToList();

            if (invalidIds.Any())
                throw new DomainException(
                    $"Aşağıdaki satır ID'leri bu projeye ait değil: {string.Join(", ", invalidIds)}");

            // 3. Fetch all relevant lines at once for efficiency
            var dbLines = await _context.ShipmentLines
                .Include(sl => sl.Shipment)
                .Where(sl => lineIds.Contains(sl.Id))
                .ToListAsync(cancellationToken);

            // 4. Pre-fetch stocks if any changes
            var stockIds = request.Lines
                .Where(l => l.NewLocalStockId.HasValue && l.NewLocalStockId > 0)
                .Select(l => l.NewLocalStockId!.Value)
                .Distinct()
                .ToList();
            
            var stocks = new Dictionary<int, Domain.Entities.StockMaster>();
            if (stockIds.Any())
            {
                stocks = await _context.StockMasters
                    .Where(s => stockIds.Contains(s.Id))
                    .ToDictionaryAsync(s => s.Id, cancellationToken);
            }

            foreach (var update in request.Lines)
            {
                var line = dbLines.FirstOrDefault(l => l.Id == update.ShipmentLineId);
                if (line == null) continue; // Should not happen but safer

                // Security Check: Ensure line belongs to the declared ZP Project context
                // Or at least allow editing if it matches the project logic.
                // We trust the query side to give correct IDs.
                
                // Allow updates only if Shipment is editable (Picking)
                if (line.Shipment.Status == ShipmentStatus.Delivered || 
                    line.Shipment.Status == ShipmentStatus.Cancelled)
                {
                    // Skip or Throw? Let's skip locked ones to avoid blocking the whole batch, 
                    // but maybe throw if crucial? 
                    // User expects these to save. Throwing is safer to alert user.
                    // But for "Bulk", maybe we just ignore locked ones?
                    // Let's throw to be safe for now.
                    throw new DomainException($"Sevkiyat satırı (ID: {line.Id}) değiştirilemez durumda: {line.Shipment.Status}");
                }

                bool changed = false;
                string historyDesc = "";
                var previousQty = line.DeliveredQty;

                // Update Delivered Qty / Difference Reason
                var newQty = update.DeliveredQty;

                if (newQty < 0)
                    throw new DomainException(
                        $"'{line.StockName}': Toplama miktarı negatif olamaz.");

                // Fazla toplama operasyonel olarak geçerlidir (koli tamamlama vb.).
                // Fark varsa DifferenceReason zorunlu — UI'dan gelmesi bekleniyor.
                var effectiveReason = newQty != line.OrderedQty
                    ? (update.DifferenceReason ?? "BulkEdit")
                    : null;

                var qtyChanged = line.DeliveredQty != newQty;
                // Sadece neden değişse bile (miktar aynı kalsa da) kaydet — kullanıcı
                // ekranı tekrar açıp nedeni düzenlediğinde kaybolmaması için.
                var reasonChanged = effectiveReason != null && line.DifferenceReason != effectiveReason;

                if (qtyChanged || reasonChanged)
                {
                    line.SetDeliveredQty(newQty, effectiveReason, "Micro Toplama");
                    if (qtyChanged)
                        historyDesc += $"{line.StockName}: {previousQty} → {newQty} (Sipariş: {line.OrderedQty}). ";
                    changed = true;
                }

                // Update Stock
                if (update.NewLocalStockId.HasValue && update.NewLocalStockId.Value > 0)
                {
                    if (stocks.TryGetValue(update.NewLocalStockId.Value, out var stock))
                    {
                        // Check if actually different
                        // We check by StockCode usually.
                        if (line.StockCode != stock.StockCode)
                        {
                            line.UpdateStockInfo(stock.StockCode, stock.StockName, stock.Unit, stock.Id);
                            
                            historyDesc += $"Stok değişti: {stock.StockCode}. ";
                            changed = true;
                        }
                    }
                }

                if (changed && !string.IsNullOrWhiteSpace(historyDesc))
                {
                    _context.ShipmentHistories.Add(new ShipmentHistory
                    {
                        ShipmentId      = line.ShipmentId,
                        OldStatus       = line.Shipment.Status,
                        NewStatus       = line.Shipment.Status,
                        ChangedAt       = DateTime.UtcNow,
                        ChangedByUserId = _currentUserService.UserId,
                        Description     = $"Micro Toplama: {historyDesc.TrimEnd()}"
                    });
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
