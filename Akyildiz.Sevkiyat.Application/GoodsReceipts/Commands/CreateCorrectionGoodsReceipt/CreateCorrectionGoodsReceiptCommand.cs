using Akyildiz.Sevkiyat.Application.Common.Interfaces;

namespace Akyildiz.Sevkiyat.Application.GoodsReceipts.Commands.CreateCorrectionGoodsReceipt
{
    public class CreateCorrectionGoodsReceiptCommand : IRequest<Guid>, IRequireRoles
    {
        public Guid OriginalGoodsReceiptId { get; set; }
        public string? Note { get; set; }

        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting" };
    }

    public class CreateCorrectionGoodsReceiptCommandHandler
        : IRequestHandler<CreateCorrectionGoodsReceiptCommand, Guid>
    {
        private readonly IApplicationDbContext _context;

        public CreateCorrectionGoodsReceiptCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(
            CreateCorrectionGoodsReceiptCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Orijinal GR'ı yükle
            var original = await _context.GoodsReceipts
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == request.OriginalGoodsReceiptId, cancellationToken);

            if (original == null)
                throw new NotFoundException("GoodsReceipt", request.OriginalGoodsReceiptId);

            if (original.Status != GoodsReceiptStatus.Posted)
                throw new DomainException(
                    $"Yalnızca Deftere Nakledilmiş (Posted) mal girişleri için düzeltme irsaliyesi oluşturulabilir. Mevcut durum: {original.Status}.");

            if (!original.Lines.Any())
                throw new DomainException("Orijinal mal girişinde satır bulunamadı.");

            // 2. Daha önce düzeltme yapılmış mı? (aynı orijinale ait başka bir düzeltme var mı?)
            var alreadyCorrected = await _context.GoodsReceipts
                .AnyAsync(x => x.ExternalRef == $"CORRECTION:{original.Id}", cancellationToken);

            if (alreadyCorrected)
                throw new ConflictException(
                    $"Bu mal girişi için daha önce düzeltme irsaliyesi oluşturulmuştur. (Orijinal: {original.WaybillNo})");

            // 3. Stok kayıtlarını yükle
            var stockIds = original.Lines.Select(l => l.StockMasterId).Distinct().ToList();
            var stocks = await _context.StockMasters
                .Where(s => stockIds.Contains(s.Id))
                .ToListAsync(cancellationToken);

            var stockDict = stocks.ToDictionary(s => s.Id);

            // 4. Düzeltme GR oluştur
            var correctionNote = string.IsNullOrWhiteSpace(request.Note)
                ? $"Düzeltme irsaliyesi — Orijinal: {original.WaybillNo} ({original.Id})"
                : $"{request.Note} — Orijinal: {original.WaybillNo} ({original.Id})";

            var correction = new GoodsReceipt
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = original.PurchaseOrderId,
                SupplierId = original.SupplierId,
                SupplierNameSnapshot = original.SupplierNameSnapshot,
                ReceiptDate = DateOnly.FromDateTime(DateTime.UtcNow),
                WaybillNo = $"DUZELTME-{original.WaybillNo}",
                WaybillDate = original.WaybillDate,
                Status = GoodsReceiptStatus.Posted,
                ExternalRef = $"CORRECTION:{original.Id}",
                Note = correctionNote
            };

            // 5. Düzeltme satırlarını oluştur (negatif miktarlar)
            var correctionLines = new List<GoodsReceiptLine>();

            foreach (var origLine in original.Lines)
            {
                var reversalQty = origLine.AcceptedQty ?? origLine.ReceivedQty;
                if (reversalQty <= 0) continue;

                // Stok pick edilmiş olabilir — düzeltme sonrası OnHandQty < ReservedQty durumunu önle
                if (stockDict.TryGetValue(origLine.StockMasterId, out var checkStock))
                {
                    var projectedOnHand = checkStock.OnHandQty - reversalQty;
                    if (projectedOnHand < checkStock.ReservedQty)
                    {
                        throw new DomainException(
                            $"'{checkStock.StockName}' stoku için düzeltme yapılamaz: " +
                            $"{reversalQty:G} adet geri alınmak isteniyor, ancak mevcut stoktan " +
                            $"{checkStock.ReservedQty:G} adet zaten rezerve edilmiş (picking'de). " +
                            $"Önce ilgili sevkiyatları tamamlayın veya iptal edin.");
                    }
                }

                var corrLine = new GoodsReceiptLine
                {
                    Id = Guid.NewGuid(),
                    GoodsReceiptId = correction.Id,
                    PurchaseOrderLineId = origLine.PurchaseOrderLineId,
                    StockMasterId = origLine.StockMasterId,
                    StockNameSnapshot = origLine.StockNameSnapshot,
                    UnitSnapshot = origLine.UnitSnapshot,
                    OrderedQty = origLine.OrderedQty,
                    ReceivedQty = -reversalQty,
                    AcceptedQty = -reversalQty,
                    RejectedQty = 0,
                    Note = $"Düzeltme: orijinal satır {origLine.Id}"
                };

                correctionLines.Add(corrLine);

                // 6. Stok düşümü (negatif düzeltme)
                if (stockDict.TryGetValue(origLine.StockMasterId, out var stock))
                {
                    stock.AdjustOnHand(-reversalQty);
                }

                _context.StockTransactions.Add(new StockTransaction
                {
                    StockMasterId = origLine.StockMasterId,
                    Type = StockTransactionType.GoodsInCorrection,
                    Qty = -reversalQty,
                    Reference = correction.WaybillNo,
                    Date = DateTime.UtcNow,
                    Note = $"Mal girişi düzeltmesi: {original.WaybillNo}"
                });
            }

            if (!correctionLines.Any())
                throw new DomainException("Orijinal mal girişinde düzeltmeye tabi miktar bulunamadı (tüm satırlar sıfır miktarlı).");

            correction.Lines = correctionLines;

            _context.GoodsReceipts.Add(correction);
            await _context.SaveChangesAsync(cancellationToken);

            return correction.Id;
        }
    }
}
