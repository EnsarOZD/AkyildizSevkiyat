using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.ClothingPicking
{
    public record ByContainerShipmentDto(
        int ShipmentId,
        string? ExternalOrderNumber,
        string? TalepNo,
        string ProjectName,
        string Status,
        int? PickingMode,
        bool PickingCompleted,
        bool Paused,
        int? BoxCount,
        bool Closed,
        IReadOnlyList<string> OtherContainerCodes);

    public record ByContainerDto(
        string ContainerCode,
        int ContainerType,
        IReadOnlyList<ByContainerShipmentDto> Shipments);

    /// <summary>
    /// Kapamacı QR akışı: bir arabanın AÇIK assignment'larındaki sevkiyat(lar)ı, toplama
    /// durumunu ve aynı sevkiyatın diğer açık arabalarını döner. Batch'te N sipariş dönebilir.
    /// </summary>
    public record GetByContainerQuery(string Code) : IRequest<ByContainerDto>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager", "Accounting", "Warehouse" };
    }

    public class GetByContainerQueryHandler : IRequestHandler<GetByContainerQuery, ByContainerDto>
    {
        private readonly IApplicationDbContext _context;
        public GetByContainerQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<ByContainerDto> Handle(GetByContainerQuery request, CancellationToken ct)
        {
            var code = (request.Code ?? string.Empty).Trim();
            var container = await _context.Containers.FirstOrDefaultAsync(c => c.Code == code, ct)
                ?? throw new NotFoundException($"'{code}' kodlu araba bulunamadı.");

            // Bu arabanın açık assignment'larındaki sevkiyat id'leri
            var shipmentIds = await _context.ContainerAssignments
                .Where(a => a.ContainerId == container.Id && a.ReleasedAt == null)
                .Select(a => a.ShipmentId)
                .Distinct()
                .ToListAsync(ct);

            var shipments = new List<ByContainerShipmentDto>();
            if (shipmentIds.Count > 0)
            {
                var data = await _context.Shipments
                    .Where(s => shipmentIds.Contains(s.Id))
                    .Include(s => s.Project)
                    .Include(s => s.IssOrder)
                    .ToListAsync(ct);

                // Bu sevkiyatların TÜM açık arabaları (diğerlerini bulmak için)
                var openAssignments = await _context.ContainerAssignments
                    .Where(a => shipmentIds.Contains(a.ShipmentId) && a.ReleasedAt == null)
                    .Include(a => a.Container)
                    .ToListAsync(ct);

                foreach (var s in data)
                {
                    var others = openAssignments
                        .Where(a => a.ShipmentId == s.Id && a.Container.Code != container.Code)
                        .Select(a => a.Container.Code)
                        .ToList();

                    shipments.Add(new ByContainerShipmentDto(
                        s.Id,
                        s.IssOrder?.ExternalOrderNumber,
                        s.TalepNo,
                        s.Project.Name,
                        s.Status.ToString(),
                        (int?)s.PickingMode,
                        s.PickingCompletedAt != null,
                        s.PickingPausedAt != null,
                        s.BoxCount,
                        s.ClosedAt != null,
                        others));
                }
            }

            return new ByContainerDto(container.Code, (int)container.Type, shipments);
        }
    }
}
