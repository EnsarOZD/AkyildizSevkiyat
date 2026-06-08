using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.ClothingPreparation
{
    public record ClothingPickLineViewDto(
        int LineId,
        string StockCode,
        string StockName,
        decimal OrderedQty,
        decimal DeliveredQty,
        string Unit,
        int? ClothingType,          // 0=Diğer, 1=Ayakkabı, null=belirsiz
        string? DifferenceReason);

    public record ClothingPickListDto(
        int ShipmentId,
        string? ExternalOrderNumber,
        string? TalepNo,
        string ProjectName,
        string Status,
        string? KoliCount,
        IReadOnlyList<ClothingPickLineViewDto> Lines);

    public record GetClothingPickListQuery(int ShipmentId) : IRequest<ClothingPickListDto>;

    public class GetClothingPickListQueryHandler : IRequestHandler<GetClothingPickListQuery, ClothingPickListDto>
    {
        private readonly IApplicationDbContext _context;
        public GetClothingPickListQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<ClothingPickListDto> Handle(GetClothingPickListQuery request, CancellationToken cancellationToken)
        {
            var s = await _context.Shipments
                .Include(x => x.Project)
                .Include(x => x.IssOrder)
                .Include(x => x.Lines).ThenInclude(l => l.StockMaster)
                .FirstOrDefaultAsync(x => x.Id == request.ShipmentId, cancellationToken)
                ?? throw new NotFoundException("Shipment", request.ShipmentId);

            var lines = s.Lines
                .OrderBy(l => (int?)(l.StockMaster != null ? l.StockMaster.ClothingType : null) == 1 ? 0 : 1) // Ayakkabı önce
                .ThenBy(l => l.StockName)
                .Select(l => new ClothingPickLineViewDto(
                    l.Id,
                    l.StockCode,
                    l.StockName,
                    l.OrderedQty,
                    l.DeliveredQty,
                    l.Unit.ToString(),
                    (int?)(l.StockMaster != null ? l.StockMaster.ClothingType : null),
                    l.DifferenceReason))
                .ToList();

            return new ClothingPickListDto(
                s.Id,
                s.IssOrder?.ExternalOrderNumber,
                s.TalepNo,
                s.Project.Name,
                s.Status.ToString(),
                s.KoliCount,
                lines);
        }
    }
}
