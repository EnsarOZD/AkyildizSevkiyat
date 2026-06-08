using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.ClothingPreparation
{
    public record ClothingPrepShipmentDto(
        int ShipmentId,
        string? ExternalOrderNumber,
        string? TalepNo,
        string ProjectCode,
        string ProjectName,
        string Status,
        DateTime DeliveryDate,
        int LineCount,
        string? PreparedByUserName,
        string? KoliCount,
        bool NetsisTransferred);

    /// <summary>Kıyafet depo hazırlık panosu: hazırlanacak (Created/Picking) ve hazır (ReadyForDispatch) kıyafet sevkiyatları.</summary>
    public record GetClothingPrepDashboardQuery() : IRequest<List<ClothingPrepShipmentDto>>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting", "Warehouse" };
    }

    public class GetClothingPrepDashboardQueryHandler : IRequestHandler<GetClothingPrepDashboardQuery, List<ClothingPrepShipmentDto>>
    {
        private readonly IApplicationDbContext _context;
        public GetClothingPrepDashboardQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<List<ClothingPrepShipmentDto>> Handle(GetClothingPrepDashboardQuery request, CancellationToken cancellationToken)
        {
            var statuses = new[] { ShipmentStatus.Created, ShipmentStatus.Picking, ShipmentStatus.ReadyForDispatch };

            return await _context.Shipments
                .Where(s => s.OperationType == OperationType.Clothing && statuses.Contains(s.Status))
                .Include(s => s.Project)
                .Include(s => s.IssOrder)
                .OrderBy(s => s.Status).ThenBy(s => s.DeliveryDate)
                .Select(s => new ClothingPrepShipmentDto(
                    s.Id,
                    s.IssOrder != null ? s.IssOrder.ExternalOrderNumber : null,
                    s.TalepNo,
                    s.Project.Code,
                    s.Project.Name,
                    s.Status.ToString(),
                    s.DeliveryDate,
                    s.Lines.Count,
                    s.PreparedByUserName,
                    s.KoliCount,
                    s.NetsisTransferredAt != null))
                .ToListAsync(cancellationToken);
        }
    }
}
