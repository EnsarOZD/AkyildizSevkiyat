using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Application.GoodsReceipts.Commands.CancelGoodsReceipt
{
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
                throw new DomainException($"Cannot cancel a Posted Goods Receipt (Id: {request.Id}). Post is immutable.");
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
