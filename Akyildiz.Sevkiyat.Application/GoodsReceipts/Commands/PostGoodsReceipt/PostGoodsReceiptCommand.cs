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
                throw new DomainException("Bu irsaliye zaten işleme alınmış.");

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

            // 2. Identify and Update All Linked Purchase Orders
            var linkedPoLineIds = entity.Lines
                .Where(l => l.PurchaseOrderLineId.HasValue)
                .Select(l => l.PurchaseOrderLineId!.Value)
                .Distinct()
                .ToList();

            var poIds = await _context.PurchaseOrderLines
                .Where(pol => linkedPoLineIds.Contains(pol.Id))
                .Select(pol => pol.PurchaseOrderId)
                .Distinct()
                .ToListAsync(cancellationToken);

            // Also add the header-level PurchaseOrderId if it exists but wasn't in the lines (unlikely but safe)
            if (entity.PurchaseOrderId.HasValue && !poIds.Contains(entity.PurchaseOrderId.Value))
            {
                poIds.Add(entity.PurchaseOrderId.Value);
            }

            if (poIds.Any())
            {
                var linkedPos = await _context.PurchaseOrders
                    .Include(p => p.Lines)
                    .Where(p => poIds.Contains(p.Id))
                    .ToListAsync(cancellationToken);

                foreach (var po in linkedPos)
                {
                    if (po.Status == PurchaseOrderStatus.Cancelled || po.Status == PurchaseOrderStatus.Closed)
                        continue; // Skip closed or cancelled POs

                    if (po.Status == PurchaseOrderStatus.Approved)
                        po.Status = PurchaseOrderStatus.PartiallyReceived;

                    // Calculate total fulfillment for this specific PO
                    var poLines = po.Lines.ToList();
                    var poLineIdsForThisPo = poLines.Select(l => l.Id).ToList();

                    // 1. Get previously POSTED quantities
                    var pastReceivedTotals = await _context.GoodsReceiptLines
                        .Where(l => l.PurchaseOrderLineId.HasValue
                                 && poLineIdsForThisPo.Contains(l.PurchaseOrderLineId!.Value)
                                 && l.GoodsReceipt.Status == GoodsReceiptStatus.Posted
                                 && l.GoodsReceiptId != entity.Id) // Exclude current if somehow already posted
                        .GroupBy(l => l.PurchaseOrderLineId!.Value)
                        .Select(g => new { PoLineId = g.Key, TotalAccepted = g.Sum(l => l.AcceptedQty ?? l.ReceivedQty) })
                        .ToListAsync(cancellationToken);

                    // 2. Add quantities from the current Goods Receipt
                    var currentGrLinesForThisPo = entity.Lines
                        .Where(l => l.PurchaseOrderLineId.HasValue && poLineIdsForThisPo.Contains(l.PurchaseOrderLineId.Value))
                        .ToList();

                    bool allLinesFulfilled = true;
                    foreach (var poLine in poLines)
                    {
                        var pastQty = pastReceivedTotals.FirstOrDefault(r => r.PoLineId == poLine.Id)?.TotalAccepted ?? 0;
                        var currentQty = currentGrLinesForThisPo
                            .Where(l => l.PurchaseOrderLineId == poLine.Id)
                            .Sum(l => l.AcceptedQty ?? l.ReceivedQty);

                        if (pastQty + currentQty < poLine.OrderedQty)
                        {
                            allLinesFulfilled = false;
                            break;
                        }
                    }

                    if (allLinesFulfilled && poLines.Any())
                    {
                        po.Status = PurchaseOrderStatus.Closed;
                    }
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
