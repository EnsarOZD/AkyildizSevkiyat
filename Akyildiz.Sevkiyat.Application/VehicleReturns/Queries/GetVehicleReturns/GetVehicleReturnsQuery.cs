using Akyildiz.Sevkiyat.Application.Common.Models;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.VehicleReturns.Queries.GetVehicleReturns
{
    public record GetVehicleReturnsQuery(
        Guid? SessionId = null,
        VehicleReturnLineStatus? LineStatus = null,
        DateTime? FromDate = null,
        DateTime? ToDate = null,
        int PageNumber = 1,
        int PageSize = 20
    ) : IRequest<PaginatedList<VehicleReturnDto>>;

    public class VehicleReturnLineDto
    {
        public int Id { get; set; }
        public int? StockMasterId { get; set; }
        public string StockCode { get; set; } = string.Empty;
        public string StockName { get; set; } = string.Empty;
        public bool IsLinkedToStock { get; set; }
        public decimal Qty { get; set; }
        public string? Note { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? LinkedShipmentId { get; set; }
        public string? LinkedShipmentIrsaliyeNo { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    public class VehicleReturnDto
    {
        public int Id { get; set; }
        public Guid DriverSessionId { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;
        public DateTime ReturnDate { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalLines { get; set; }
        public int PendingLines { get; set; }
        public List<VehicleReturnLineDto> Lines { get; set; } = new();
    }

    public class GetVehicleReturnsQueryHandler : IRequestHandler<GetVehicleReturnsQuery, PaginatedList<VehicleReturnDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetVehicleReturnsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedList<VehicleReturnDto>> Handle(GetVehicleReturnsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.VehicleReturns
                .Include(vr => vr.DriverSession)
                    .ThenInclude(ds => ds.Driver)
                .Include(vr => vr.DriverSession)
                    .ThenInclude(ds => ds.Vehicle)
                .Include(vr => vr.Lines)
                    .ThenInclude(l => l.StockMaster)
                .Include(vr => vr.Lines)
                    .ThenInclude(l => l.LinkedShipment)
                .AsQueryable();

            if (request.SessionId.HasValue)
                query = query.Where(vr => vr.DriverSessionId == request.SessionId.Value);

            if (request.FromDate.HasValue)
                query = query.Where(vr => vr.ReturnDate >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                query = query.Where(vr => vr.ReturnDate <= request.ToDate.Value);

            if (request.LineStatus.HasValue)
                query = query.Where(vr => vr.Lines.Any(l => l.Status == request.LineStatus.Value));

            var projected = query
                .OrderByDescending(vr => vr.ReturnDate)
                .Select(vr => new VehicleReturnDto
                {
                    Id = vr.Id,
                    DriverSessionId = vr.DriverSessionId,
                    DriverName = vr.DriverSession.Driver.FullName,
                    PlateNumber = vr.DriverSession.Vehicle.PlateNumber,
                    ReturnDate = vr.ReturnDate,
                    Note = vr.Note,
                    CreatedAt = vr.CreatedAt,
                    TotalLines = vr.Lines.Count,
                    PendingLines = vr.Lines.Count(l => l.Status == VehicleReturnLineStatus.Pending),
                    Lines = vr.Lines.Select(l => new VehicleReturnLineDto
                    {
                        Id = l.Id,
                        StockMasterId = l.StockMasterId,
                        StockCode = l.StockMaster != null ? l.StockMaster.StockCode : (l.StockCodeFree ?? ""),
                        StockName = l.StockMaster != null ? l.StockMaster.StockName : (l.StockNameFree ?? ""),
                        IsLinkedToStock = l.StockMasterId.HasValue,
                        Qty = l.Qty,
                        Note = l.Note,
                        Status = l.Status.ToString(),
                        LinkedShipmentId = l.LinkedShipmentId,
                        LinkedShipmentIrsaliyeNo = l.LinkedShipment != null ? l.LinkedShipment.IrsaliyeNo : null,
                        ResolvedAt = l.ResolvedAt
                    }).ToList()
                });

            return await PaginatedList<VehicleReturnDto>.CreateAsync(projected, request.PageNumber, request.PageSize);
        }
    }
}
