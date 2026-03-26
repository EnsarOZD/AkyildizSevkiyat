using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.GoodsReceipts.Commands.PostGoodsReceipt
{
    public class PostGoodsReceiptCommand : IRequest<Unit>, IRequireRoles
    {
        public Guid Id { get; set; }

        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Warehouse" };
    }

    public class PostGoodsReceiptCommandHandler : IRequestHandler<PostGoodsReceiptCommand, Unit>
    {
        private readonly IApplicationDbContext _context;

        public PostGoodsReceiptCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(PostGoodsReceiptCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.GoodsReceipts
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (entity == null) throw new NotFoundException("GoodsReceipt", request.Id);

            // 1. Idempotency Check
            if (entity.Status == GoodsReceiptStatus.Posted)
                return Unit.Value;

            if (entity.Status != GoodsReceiptStatus.Draft)
                throw new DomainException($"Cannot post Goods Receipt in status {entity.Status}. Only Draft can be posted.");

            if (!entity.Lines.Any())
                throw new DomainException("Cannot post an empty Goods Receipt.");

            // Kabul miktarı teslim alınan miktarı aşamaz
            var overAcceptedLines = entity.Lines
                .Where(l => l.AcceptedQty.HasValue && l.AcceptedQty > l.ReceivedQty)
                .ToList();

            if (overAcceptedLines.Any())
            {
                var lineRefs = string.Join(", ", overAcceptedLines.Select(l =>
                    $"{l.StockNameSnapshot ?? l.StockMasterId.ToString()} (kabul: {l.AcceptedQty}, teslim: {l.ReceivedQty})"));
                throw new DomainException($"Kabul miktarı teslim alınan miktarı aşamaz: {lineRefs}");
            }

            // T09: Kısmi kabul validasyonu — fark varsa Red Nedeni zorunlu
            var invalidLines = entity.Lines
                .Where(l => l.AcceptedQty.HasValue && l.AcceptedQty < l.ReceivedQty && string.IsNullOrWhiteSpace(l.RejectReason))
                .ToList();

            if (invalidLines.Any())
            {
                var lineRefs = string.Join(", ", invalidLines.Select(l => l.StockNameSnapshot ?? l.StockMasterId.ToString()));
                throw new DomainException($"Kabul miktarı teslim alınan miktardan az olan satırlarda red nedeni girilmesi zorunludur: {lineRefs}");
            }

            // 2. Check Linked Purchase Order
            if (entity.PurchaseOrderId.HasValue)
            {
                var po = await _context.PurchaseOrders
                    .Include(p => p.Lines)
                    .FirstOrDefaultAsync(p => p.Id == entity.PurchaseOrderId.Value, cancellationToken);

                if (po != null)
                {
                    if (po.Status == PurchaseOrderStatus.Cancelled || po.Status == PurchaseOrderStatus.Closed)
                        throw new DomainException($"Cannot post Receipt because linked Purchase Order is {po.Status}.");

                    if (po.Status == PurchaseOrderStatus.Approved)
                        po.Status = PurchaseOrderStatus.PartiallyReceived;

                    // PO tamamen teslim alındı mı? Tüm posted GR satırlarının toplamını kontrol et
                    var poLineIds = po.Lines.Select(l => l.Id).ToList();
                    var receivedTotals = await _context.GoodsReceiptLines
                        .Where(l => l.PurchaseOrderLineId.HasValue
                                 && poLineIds.Contains(l.PurchaseOrderLineId!.Value)
                                 && l.GoodsReceipt.Status == GoodsReceiptStatus.Posted)
                        .GroupBy(l => l.PurchaseOrderLineId!.Value)
                        .Select(g => new { PoLineId = g.Key, TotalAccepted = g.Sum(l => l.AcceptedQty ?? l.ReceivedQty) })
                        .ToListAsync(cancellationToken);

                    // Mevcut GR'ın kabul miktarlarını da ekle (henüz saved değil)
                    foreach (var grLine in entity.Lines.Where(l => l.PurchaseOrderLineId.HasValue))
                    {
                        var incoming = grLine.AcceptedQty ?? grLine.ReceivedQty;
                        var existing = receivedTotals.FirstOrDefault(r => r.PoLineId == grLine.PurchaseOrderLineId!.Value);
                        if (existing != null)
                            receivedTotals[receivedTotals.IndexOf(existing)] = new { existing.PoLineId, TotalAccepted = existing.TotalAccepted + incoming };
                        else
                            receivedTotals.Add(new { PoLineId = grLine.PurchaseOrderLineId!.Value, TotalAccepted = incoming });
                    }

                    bool fullyReceived = po.Lines.All(poLine =>
                        receivedTotals.FirstOrDefault(r => r.PoLineId == poLine.Id)?.TotalAccepted >= poLine.OrderedQty);

                    if (fullyReceived)
                        po.Status = PurchaseOrderStatus.Closed;
                }
            }

            // 3. Stok Güncellemesi
            var stockIds = entity.Lines.Select(l => l.StockMasterId).Distinct().ToList();
            var stocks = await _context.StockMasters
                .Where(s => stockIds.Contains(s.Id))
                .ToListAsync(cancellationToken);

            var stockDict = stocks.ToDictionary(s => s.Id);

            foreach (var line in entity.Lines)
            {
                // AcceptedQty varsa onu kullan, yoksa ReceivedQty
                var incomingQty = line.AcceptedQty ?? line.ReceivedQty;
                if (incomingQty <= 0) continue;

                if (stockDict.TryGetValue(line.StockMasterId, out var stock))
                {
                    stock.Increase(incomingQty);
                }

                _context.StockTransactions.Add(new StockTransaction
                {
                    StockMasterId = line.StockMasterId,
                    Type = StockTransactionType.GoodsIn,
                    Qty = incomingQty,
                    Reference = entity.WaybillNo,
                    Date = DateTime.UtcNow,
                    Note = $"Mal girişi: {entity.WaybillNo}"
                });
            }

            entity.Status = GoodsReceiptStatus.Posted;

            // Future: Netsis Integration Hook here

            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
