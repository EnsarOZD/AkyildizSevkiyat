using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.ClothingPicking
{
    /// <summary>
    /// Etiket elle yazıldı (K8) — yazıcıya gönderilmeden LabelPrinted işaretle.
    /// </summary>
    public record MarkLabelHandwrittenCommand(int ShipmentId) : IRequest<Unit>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager", "Accounting", "Warehouse" };
    }

    public class MarkLabelHandwrittenCommandHandler : IRequestHandler<MarkLabelHandwrittenCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        public MarkLabelHandwrittenCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Unit> Handle(MarkLabelHandwrittenCommand request, CancellationToken ct)
        {
            var s = await _context.Shipments.FirstOrDefaultAsync(x => x.Id == request.ShipmentId, ct)
                ?? throw new NotFoundException("Shipment", request.ShipmentId);

            s.LabelPrinted = true;
            s.LabelPrintedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return Unit.Value;
        }
    }
}
