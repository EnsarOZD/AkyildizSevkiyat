using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.ClothingPreparation
{
    public record AggLineRefDto(int ShipmentId, int LineId, decimal OrderedQty);

    public record ClothingAggProductDto(
        string StockCode,
        string StockName,
        string Unit,
        int? ClothingType,
        decimal TotalOrderedQty,
        decimal TotalDeliveredQty,
        IReadOnlyList<AggLineRefDto> Refs);

    public record ClothingAggShipmentDto(
        int ShipmentId, string? ExternalOrderNumber, string? TalepNo, string ProjectName, string? KoliCount, string Status);

    public record ClothingAggregatePickListDto(
        IReadOnlyList<ClothingAggShipmentDto> Shipments,
        IReadOnlyList<ClothingAggProductDto> Products);

    /// <summary>Seçilen kıyafet sevkiyatlarının satırlarını stok bazında toplar (konsolide toplama).</summary>
    public record GetClothingAggregatePickListQuery(List<int> ShipmentIds) : IRequest<ClothingAggregatePickListDto>;

    public class GetClothingAggregatePickListQueryHandler
        : IRequestHandler<GetClothingAggregatePickListQuery, ClothingAggregatePickListDto>
    {
        private readonly IApplicationDbContext _context;
        public GetClothingAggregatePickListQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<ClothingAggregatePickListDto> Handle(GetClothingAggregatePickListQuery request, CancellationToken cancellationToken)
        {
            if (request.ShipmentIds is null || request.ShipmentIds.Count == 0)
                throw new DomainException("En az bir sevkiyat seçilmelidir.");

            var shipments = await _context.Shipments
                .Where(s => request.ShipmentIds.Contains(s.Id) && s.OperationType == OperationType.Clothing)
                .Include(s => s.Project)
                .Include(s => s.IssOrder)
                .Include(s => s.Lines).ThenInclude(l => l.StockMaster)
                .OrderBy(s => s.Id)
                .ToListAsync(cancellationToken);

            if (shipments.Count == 0)
                throw new DomainException("Seçilen kıyafet sevkiyatı bulunamadı.");

            var shipmentDtos = shipments
                .Select(s => new ClothingAggShipmentDto(
                    s.Id, s.IssOrder?.ExternalOrderNumber, s.TalepNo, s.Project.Name, s.KoliCount, s.Status.ToString()))
                .ToList();

            // Stok kodu bazında topla (FIFO için sevkiyat sırasına göre ref'ler)
            var products = shipments
                .SelectMany(s => s.Lines.Select(l => new { s.Id, Line = l }))
                .GroupBy(x => x.Line.StockCode)
                .Select(g =>
                {
                    var first = g.First().Line;
                    return new ClothingAggProductDto(
                        g.Key,
                        first.StockName,
                        first.Unit.ToString(),
                        (int?)(first.StockMaster != null ? first.StockMaster.ClothingType : null),
                        g.Sum(x => x.Line.OrderedQty),
                        g.Sum(x => x.Line.DeliveredQty),
                        g.OrderBy(x => x.Id)
                         .Select(x => new AggLineRefDto(x.Id, x.Line.Id, x.Line.OrderedQty))
                         .ToList());
                })
                .OrderByDescending(p => p.ClothingType == 1) // Ayakkabı önce
                .ThenBy(p => p.StockName)
                .ToList();

            return new ClothingAggregatePickListDto(shipmentDtos, products);
        }
    }
}
