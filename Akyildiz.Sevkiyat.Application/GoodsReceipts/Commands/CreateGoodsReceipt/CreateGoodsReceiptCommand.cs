using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Application.GoodsReceipts.Commands.CreateGoodsReceipt
{
    public class CreateGoodsReceiptCommand : IRequest<CreateGoodsReceiptResponse>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting", "Warehouse" };

        public bool IgnoreDuplicateWarning { get; set; }

        public Guid? PurchaseOrderId { get; set; }
        public List<Guid>? PurchaseOrderIds { get; set; } // Multiple PO support
        public Guid? SupplierId { get; set; }
        public string SupplierNameSnapshot { get; set; } = string.Empty;
        public DateOnly ReceiptDate { get; set; }
        public string WaybillNo { get; set; } = string.Empty;
        public DateOnly WaybillDate { get; set; }
        public string? Note { get; set; }
        public string? ExternalRef { get; set; }
    }

    public class CreateGoodsReceiptResponse
    {
        public Guid?  Id { get; set; }
        public bool HasDuplicateWarning { get; set; }
        public List<DuplicateReceiptMatchDto> DuplicateMatches { get; set; } = new();
    }

    public class DuplicateReceiptMatchDto
    {
        public Guid Id { get; set; }
        public string WaybillNo { get; set; } = string.Empty;
        public DateOnly WaybillDate { get; set; }
        public string SupplierName { get; set; } = string.Empty;
    }

    public class CreateGoodsReceiptCommandHandler : IRequestHandler<CreateGoodsReceiptCommand, CreateGoodsReceiptResponse>
    {
        private readonly IApplicationDbContext _context;

        public CreateGoodsReceiptCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CreateGoodsReceiptResponse> Handle(CreateGoodsReceiptCommand request, CancellationToken cancellationToken)
        {
            // 0. Default ReceiptDate if missing
            if (request.ReceiptDate == default)
            {
                request.ReceiptDate = DateOnly.FromDateTime(DateTime.UtcNow);
            }

            // Normalize list
            var poIds = request.PurchaseOrderIds ?? new List<Guid>();
            if (request.PurchaseOrderId.HasValue && !poIds.Contains(request.PurchaseOrderId.Value))
            {
                poIds.Add(request.PurchaseOrderId.Value);
            }

            // 1. Resolve Supplier and PO Info
            if (poIds.Any())
            {
                var pos = await _context.PurchaseOrders
                    .Where(x => poIds.Contains(x.Id))
                    .ToListAsync(cancellationToken);

                if (pos.Count != poIds.Count)
                    throw new NotFoundException("PurchaseOrder", string.Join(", ", poIds));

                // Verify same supplier
                var supplierIds = pos.Select(x => x.SupplierId).Distinct().ToList();
                if (supplierIds.Count > 1)
                    throw new DomainException("Selected Purchase Orders must belong to the same supplier.");

                // Validate PO statuses — cannot receive on Closed or Cancelled orders
                foreach (var po in pos)
                {
                    if (po.Status == PurchaseOrderStatus.Closed || po.Status == PurchaseOrderStatus.Cancelled)
                        throw new DomainException($"Kapalı veya iptal edilmiş siparişe mal kabul oluşturulamaz. (Sipariş: {po.OrderNumber})");
                }

                var mainPo = pos.First();
                request.SupplierId = mainPo.SupplierId;
                request.SupplierNameSnapshot = mainPo.SupplierNameSnapshot;

                // If only one PO, link it as the primary PO
                if (poIds.Count == 1) request.PurchaseOrderId = mainPo.Id;
                else request.PurchaseOrderId = null; // Don't link a single PO if it's multiple
            }
            else
            {
                // PO-less flow
                if (!request.SupplierId.HasValue || request.SupplierId == Guid.Empty)
                    throw new DomainException("Supplier is required for PO-less goods receipt.");

                if (string.IsNullOrWhiteSpace(request.SupplierNameSnapshot))
                {
                    var supplier = await _context.Suppliers.FindAsync(new object[] { request.SupplierId.Value }, cancellationToken);
                    if (supplier == null) throw new NotFoundException("Supplier", request.SupplierId.Value);
                    request.SupplierNameSnapshot = supplier.Name;
                }

                if (string.IsNullOrWhiteSpace(request.Note))
                    throw new DomainException("Açıklama alanı (not) siparişsiz mal kabullerinde zorunludur.");
            }

            // 2. Check Duplicates (Normalized)
            var normalizedWaybillNo = request.WaybillNo.Trim().ToUpperInvariant();

            var query = _context.GoodsReceipts.AsQueryable()
                .Where(x => x.WaybillNo.ToLower() == request.WaybillNo.ToLower()
                            && x.WaybillDate == request.WaybillDate
                            && x.Status != GoodsReceiptStatus.Cancelled
                            && x.SupplierId == request.SupplierId!.Value);

            var duplicates = await query
                .Select(x => new DuplicateReceiptMatchDto 
                { 
                    Id = x.Id, 
                    WaybillNo = x.WaybillNo, 
                    WaybillDate = x.WaybillDate,
                    SupplierName = x.SupplierNameSnapshot 
                })
                .ToListAsync(cancellationToken);

            if (duplicates.Any() && !request.IgnoreDuplicateWarning)
            {
                return new CreateGoodsReceiptResponse
                {
                    Id = null,
                    HasDuplicateWarning = true,
                    DuplicateMatches = duplicates
                };
            }

            // 3. Create Receipt
            var entity = new GoodsReceipt
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = request.PurchaseOrderId,
                SupplierId = request.SupplierId!.Value,
                SupplierNameSnapshot = request.SupplierNameSnapshot,
                ReceiptDate = request.ReceiptDate,
                WaybillNo = request.WaybillNo,
                WaybillDate = request.WaybillDate,
                Status = GoodsReceiptStatus.Draft,
                Note = request.Note,
                ExternalRef = request.ExternalRef
            };

            // 4. Auto-populate Lines from SELECTED POs
            if (poIds.Any())
            {
                var poLines = await _context.PurchaseOrderLines
                    .Include(x => x.StockMaster)
                    .Where(x => poIds.Contains(x.PurchaseOrderId))
                    .ToListAsync(cancellationToken);

                foreach (var poLine in poLines)
                {
                    entity.Lines.Add(new GoodsReceiptLine
                    {
                        Id = Guid.NewGuid(),
                        GoodsReceiptId = entity.Id,
                        PurchaseOrderLineId = poLine.Id,
                        StockMasterId = poLine.StockMasterId,
                        StockNameSnapshot = poLine.StockMaster.StockName,
                        UnitSnapshot = poLine.Unit,
                        OrderedQty = poLine.OrderedQty,
                        ReceivedQty = 0,
                        AcceptedQty = 0,
                        RejectedQty = 0,
                        Note = poLine.Note
                    });
                }
            }

            _context.GoodsReceipts.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            return new CreateGoodsReceiptResponse
            {
                Id = entity.Id,
                HasDuplicateWarning = false
            };
        }
    }
}
