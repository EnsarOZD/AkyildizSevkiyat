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
            var rawLines = await _context.ShipmentLines
                .Where(sl =>
                    sl.Shipment.ZonePreparationId == request.ZonePreparationId &&
                    sl.Shipment.Status != ShipmentStatus.Cancelled &&
                    sl.Shipment.Status != ShipmentStatus.Passive &&
                    sl.OrderedQty > 0)
                .Select(sl => new
                {
                    LineId          = sl.Id,
                    sl.OrderedQty,
                    sl.DeliveredQty,
                    sl.DifferenceReason,
                    StockCode       = sl.StockMaster != null ? sl.StockMaster.StockCode : sl.StockCode,
                    StockName       = sl.StockMaster != null ? sl.StockMaster.StockName : sl.StockName,
                    Unit            = sl.StockMaster != null ? sl.StockMaster.Unit.ToString() : sl.Unit.ToString(),
                    ShipmentId      = sl.Shipment.Id,
                    TalepNo         = sl.Shipment.TalepNo,
                    IrsaliyeNo      = sl.Shipment.IrsaliyeNo,
                    ProjectId       = sl.Shipment.ProjectId,
                    ProjectName     = sl.Shipment.Project.Name,
                    ProjectCode     = sl.Shipment.Project.Code
                })
                .OrderBy(sl => sl.ProjectId)
                .ThenBy(sl => sl.ShipmentId)
                .ThenBy(sl => sl.StockName)
                .ToListAsync(cancellationToken);

            var result = rawLines
                .GroupBy(sl => sl.ShipmentId)
                .Select(g =>
                {
                    var first = g.First();
                    return new VerificationShipmentDto(
                        first.ShipmentId,
                        first.TalepNo,
                        first.IrsaliyeNo,
                        first.ProjectId,
                        first.ProjectName,
                        first.ProjectCode,
                        g.Select(l => new VerificationLineDto(
                            l.LineId,
                            l.StockCode,
                            l.StockName,
                            l.Unit,
                            l.OrderedQty,
                            l.DeliveredQty,
                            l.DeliveredQty - l.OrderedQty,
                            l.DifferenceReason
                        )).ToList()
                    );
                })
                .ToList();

            return result;
        }
    }
}
