using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.ClothingPicking
{
    public record PickingDetailLineDto(
        int LineId,
        string StockCode,
        string StockName,
        decimal OrderedQty,
        decimal DeliveredQty,
        decimal DifferenceQty,
        string Unit,
        int? ClothingType,           // 0=Diğer, 1=Ayakkabı, null=belirsiz
        string? DifferenceReason);

    public record PickingDetailDto(
        int ShipmentId,
        string? AssignedPickerName,   // claim eden toplayıcı
        string? PreparedByUserName,   // toplamayı bitiren
        DateTime? PreparedAt,
        string? ClosedByUserName,     // kapamayı yapan
        DateTime? ClosedAt,
        int? BoxCount,                // koli adedi (tek doğruluk)
        int? PackageType,             // 0=Koli, 1=Poşet
        bool LabelPrinted,
        IReadOnlyList<string> OpenContainerCodes,
        IReadOnlyList<PickingDetailLineDto> Lines);

    /// <summary>
    /// Yönetici panosunda bir kıyafet sevkiyatının satır detayı (lazy-load). Liste sorgusunu
    /// (GetPickingOverview) şişirmemek için ayrı; satırlar + toplayıcı/kapamacı/koli bilgisi.
    /// </summary>
    public record GetPickingDetailQuery(int ShipmentId) : IRequest<PickingDetailDto>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager" };
    }

    public class GetPickingDetailQueryHandler : IRequestHandler<GetPickingDetailQuery, PickingDetailDto>
    {
        private readonly IApplicationDbContext _context;
        public GetPickingDetailQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<PickingDetailDto> Handle(GetPickingDetailQuery request, CancellationToken ct)
        {
            var s = await _context.Shipments
                .Include(x => x.Lines).ThenInclude(l => l.StockMaster)
                .FirstOrDefaultAsync(x => x.Id == request.ShipmentId, ct)
                ?? throw new NotFoundException("Shipment", request.ShipmentId);

            var openContainerCodes = await _context.ContainerAssignments
                .Where(a => a.ShipmentId == s.Id && a.ReleasedAt == null)
                .OrderBy(a => a.AssignedAt)
                .Select(a => a.Container.Code)
                .ToListAsync(ct);

            var lines = s.Lines
                .OrderBy(l => (l.StockMaster != null ? (int?)l.StockMaster.ClothingType : null) == 1 ? 0 : 1) // Ayakkabı önce
                .ThenBy(l => l.StockName)
                .Select(l => new PickingDetailLineDto(
                    l.Id,
                    l.StockCode,
                    l.StockName,
                    l.OrderedQty,
                    l.DeliveredQty,
                    l.DifferenceQty,
                    l.Unit.ToString(),
                    l.StockMaster != null ? (int?)l.StockMaster.ClothingType : null,
                    l.DifferenceReason))
                .ToList();

            return new PickingDetailDto(
                s.Id,
                s.AssignedPickerName,
                s.PreparedByUserName,
                s.PreparedAt,
                s.ClosedByUserName,
                s.ClosedAt,
                s.BoxCount,
                (int?)s.PackageType,
                s.LabelPrinted,
                openContainerCodes,
                lines);
        }
    }
}
