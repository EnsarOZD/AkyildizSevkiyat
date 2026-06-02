using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Queries.GetZoneVerification
{
    public record VerificationLineDto(
        int ShipmentLineId,
        string StockCode,
        string StockName,
        string Unit,
        decimal OrderedQty,
        decimal DeliveredQty,
        decimal Difference,
        string? DifferenceReason
    );

    public record VerificationShipmentDto(
        int ShipmentId,
        string? TalepNo,
        string? OrderNumber,
        string? IrsaliyeNo,
        int ProjectId,
        string ProjectName,
        string ProjectCode,
        List<VerificationLineDto> Lines
    );

    public record GetZoneVerificationQuery(int ZonePreparationId) : IRequest<List<VerificationShipmentDto>>;

    public class GetZoneVerificationQueryHandler : IRequestHandler<GetZoneVerificationQuery, List<VerificationShipmentDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetZoneVerificationQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<VerificationShipmentDto>> Handle(GetZoneVerificationQuery request, CancellationToken cancellationToken)
        {
            // Sevkiyat-bazlı sorgu: satırı/miktarı olmayan sevkiyatlar da listede görünsün
            // (önceki satır-bazlı sorgu OrderedQty>0 satırı olmayan sevkiyatları gizliyordu).
            var shipments = await _context.Shipments
                .Where(s =>
                    s.ZonePreparationId == request.ZonePreparationId &&
                    s.Status != ShipmentStatus.Cancelled &&
                    s.Status != ShipmentStatus.Passive)
                .OrderBy(s => s.ProjectId)
                .ThenBy(s => s.Id)
                .Select(s => new
                {
                    ShipmentId  = s.Id,
                    s.TalepNo,
                    OrderNumber = s.IssOrder != null ? s.IssOrder.ExternalOrderNumber : null,
                    s.IrsaliyeNo,
                    s.ProjectId,
                    ProjectName = s.Project.Name,
                    ProjectCode = s.Project.Code,
                    Lines = s.Lines
                        .Where(l => l.OrderedQty > 0)
                        .Select(l => new VerificationLineDto(
                            l.Id,
                            l.StockMaster != null ? l.StockMaster.StockCode : l.StockCode,
                            l.StockMaster != null ? l.StockMaster.StockName : l.StockName,
                            l.StockMaster != null ? l.StockMaster.Unit.ToString() : l.Unit.ToString(),
                            l.OrderedQty,
                            l.DeliveredQty,
                            l.DeliveredQty - l.OrderedQty,
                            l.DifferenceReason))
                        .ToList()
                })
                .ToListAsync(cancellationToken);

            return shipments
                .Select(s => new VerificationShipmentDto(
                    s.ShipmentId,
                    s.TalepNo,
                    s.OrderNumber,
                    s.IrsaliyeNo,
                    s.ProjectId,
                    s.ProjectName,
                    s.ProjectCode,
                    s.Lines.OrderBy(l => l.StockName).ToList()))
                .ToList();
        }
    }
}
