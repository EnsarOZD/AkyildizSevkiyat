using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Application.GoodsReceipts.Commands.CreateGoodsReceipt
{
    public class CreateGoodsReceiptCommand : IRequest<CreateGoodsReceiptResponse>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Warehouse" };

        public bool IgnoreDuplicateWarning { get; set; }

        public Guid? PurchaseOrderId { get; set; }
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
            // 0. Default ReceiptDate if missing (Default value in struct is 0001-01-01)
            if (request.ReceiptDate == default)
            {
                request.ReceiptDate = DateOnly.FromDateTime(DateTime.UtcNow);
            }

            // 1. Resolve Supplier from PO (PO is now mandatory)
            if (!request.PurchaseOrderId.HasValue || request.PurchaseOrderId.Value == Guid.Empty)
            {
                throw new DomainException("Purchase Order selection is required.");
            }

            var po = await _context.PurchaseOrders.FindAsync(new object[] { request.PurchaseOrderId.Value }, cancellationToken);
            if (po == null) throw new NotFoundException("PurchaseOrder", request.PurchaseOrderId.Value);
                
            // Derived from PO
            request.SupplierNameSnapshot = po.SupplierNameSnapshot;
            request.SupplierId = po.SupplierId;

            // 2. Check Duplicates (Normalized)
            // Criteria: Supplier (Id or Normalized Name) + WaybillNo + WaybillDate
            // Existing receipts that are NOT Cancelled.
            
            var normalizedSupplierName = request.SupplierNameSnapshot.Trim().ToUpperInvariant();
            var normalizedWaybillNo = request.WaybillNo.Trim().ToUpperInvariant();

            var query = _context.GoodsReceipts.AsQueryable()
                .Where(x => x.WaybillNo == request.WaybillNo 
                            && x.WaybillDate == request.WaybillDate
                            && x.Status != GoodsReceiptStatus.Cancelled);

            // Fetch potential matches to memory for string comparison if needed, OR try to stick to SQL.
            // Since we want robust check, let's filter by SupplierId if present, else Name.
            
            query = query.Where(x => x.SupplierId == request.SupplierId!.Value);

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

            if (!request.SupplierId.HasValue)
            {
                throw new DomainException("Supplier is required.");
            }

            // 3. Create Receipt
            var entity = new GoodsReceipt
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = request.PurchaseOrderId,
                SupplierId = request.SupplierId.Value,
                SupplierNameSnapshot = request.SupplierNameSnapshot, // Already resolved
                ReceiptDate = request.ReceiptDate,
                WaybillNo = request.WaybillNo,
                WaybillDate = request.WaybillDate,
                Status = GoodsReceiptStatus.Draft,
                Note = request.Note,
                ExternalRef = request.ExternalRef
            };

            // 4. Auto-populate Lines from PO if present
            if (request.PurchaseOrderId.HasValue)
            {
                var poLines = await _context.PurchaseOrderLines
                    .Include(x => x.StockMaster)
                    .Where(x => x.PurchaseOrderId == request.PurchaseOrderId.Value)
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
                        ReceivedQty = 0, // User will fill this
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
