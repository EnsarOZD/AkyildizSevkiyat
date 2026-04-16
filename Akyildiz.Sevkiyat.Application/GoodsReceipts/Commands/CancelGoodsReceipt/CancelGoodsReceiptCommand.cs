using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Application.GoodsReceipts.Commands.CancelGoodsReceipt
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Akyildiz.Sevkiyat.Application.Common.Interfaces;
    using Akyildiz.Sevkiyat.Domain.Entities;
    using Akyildiz.Sevkiyat.Domain.Enums;
    using Akyildiz.Sevkiyat.Domain.Exceptions;
    using MediatR;
    using Microsoft.EntityFrameworkCore;
    public class CancelGoodsReceiptCommand : IRequest<Unit>, IRequireRoles
    {
        public Guid Id { get; set; }

        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager" };
    }

    public class CancelGoodsReceiptCommandHandler : IRequestHandler<CancelGoodsReceiptCommand, Unit>
    {
        private readonly IApplicationDbContext _context;

        public CancelGoodsReceiptCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(CancelGoodsReceiptCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.GoodsReceipts
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
            
            if (entity == null) throw new NotFoundException("GoodsReceipt", request.Id);

            if (entity.Status == GoodsReceiptStatus.Posted)
            {
                var stockIds = entity.Lines.Select(l => l.StockMasterId).Distinct().ToList();
                var stocks = await _context.StockMasters
                    .Where(s => stockIds.Contains(s.Id))
                    .ToListAsync(cancellationToken);

                var stockDict = stocks.ToDictionary(s => s.Id);

                // 1. Stock validation (Negative stock check)
                foreach(var line in entity.Lines)
                {
                    var incomingQty = line.AcceptedQty ?? line.ReceivedQty;
                    if (incomingQty <= 0) continue;

                    if (stockDict.TryGetValue(line.StockMasterId, out var stock))
                    {
                        if (stock.AvailableQty - incomingQty < 0)
                        {
                            throw new DomainException($"Stok yetersizliği nedeniyle ({stock.StockName} stok kodu için bakiye eksiye düşeceği için) bu irsaliye iptal edilemez.");
                        }
                    }
                }

                // 2. Adjust Stock
                foreach(var line in entity.Lines)
                {
                    var incomingQty = line.AcceptedQty ?? line.ReceivedQty;
                    if (incomingQty <= 0) continue;

                    if (stockDict.TryGetValue(line.StockMasterId, out var stock))
                    {
                        stock.AdjustOnHand(-incomingQty);
                    }

                    _context.StockTransactions.Add(new StockTransaction
                    {
                        StockMasterId = line.StockMasterId,
                        Type = StockTransactionType.GoodsOut,
                        Qty = incomingQty,
                        Reference = "İPTAL-" + entity.WaybillNo,
                        Date = DateTime.UtcNow,
                        Note = $"İptal: {entity.WaybillNo}"
                    });
                }

                // 3. Purchase Order Update
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

                if (entity.PurchaseOrderId.HasValue && !poIds.Contains(entity.PurchaseOrderId.Value))
                {
                    poIds.Add(entity.PurchaseOrderId.Value);
                }

                if (poIds.Any())
                {
                    var linkedPos = await _context.PurchaseOrders
                        .Where(p => poIds.Contains(p.Id))
                        .ToListAsync(cancellationToken);

                    foreach (var po in linkedPos)
                    {
                        if (po.Status == PurchaseOrderStatus.Cancelled) continue;
                        
                        po.Status = PurchaseOrderStatus.PartiallyReceived; 
                    }
                }
            }

            if (entity.Status == GoodsReceiptStatus.Cancelled)
            {
                 // Already cancelled. Return success (Idempotent).
                 return Unit.Value;
            }

            if (entity.Status != GoodsReceiptStatus.Draft)
            {
                throw new DomainException($"Cannot cancel Goods Receipt in status {entity.Status}. Only Draft can be cancelled.");
            }

            entity.Status = GoodsReceiptStatus.Cancelled;
            
            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
