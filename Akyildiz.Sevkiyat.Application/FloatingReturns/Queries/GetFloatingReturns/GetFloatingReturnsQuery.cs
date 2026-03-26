using Akyildiz.Sevkiyat.Application.Common.Models;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.FloatingReturns.Queries.GetFloatingReturns
{
    public record GetFloatingReturnsQuery(
        FloatingReturnStatus? Status = null,
        DateTime? FromDate = null,
        DateTime? ToDate = null,
        int PageNumber = 1,
        int PageSize = 20
    ) : IRequest<PaginatedList<FloatingReturnDto>>;

    public class FloatingReturnDto
    {
        public int Id { get; set; }
        public DateTime ReturnDate { get; set; }
        public string StockCode { get; set; } = string.Empty;
        public string StockName { get; set; } = string.Empty;
        public bool IsLinkedToStock { get; set; }
        public decimal Qty { get; set; }
        public string ReturnReason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Note { get; set; }
        public int? LinkedShipmentId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    public class GetFloatingReturnsQueryHandler : IRequestHandler<GetFloatingReturnsQuery, PaginatedList<FloatingReturnDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetFloatingReturnsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedList<FloatingReturnDto>> Handle(GetFloatingReturnsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.FloatingReturns
                .Include(f => f.StockMaster)
                .AsQueryable();

            if (request.Status.HasValue)
                query = query.Where(f => f.Status == request.Status.Value);

            if (request.FromDate.HasValue)
                query = query.Where(f => f.ReturnDate >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                query = query.Where(f => f.ReturnDate <= request.ToDate.Value);

            var projected = query
                .OrderByDescending(f => f.ReturnDate)
                .Select(f => new FloatingReturnDto
                {
                    Id = f.Id,
                    ReturnDate = f.ReturnDate,
                    StockCode = f.StockMaster != null ? f.StockMaster.StockCode : (f.StockCodeFree ?? ""),
                    StockName = f.StockMaster != null ? f.StockMaster.StockName : (f.StockNameFree ?? ""),
                    IsLinkedToStock = f.StockMasterId.HasValue,
                    Qty = f.Qty,
                    ReturnReason = f.ReturnReason.ToString(),
                    Status = f.Status.ToString(),
                    Note = f.Note,
                    LinkedShipmentId = f.LinkedShipmentId,
                    CreatedAt = f.CreatedAt,
                    ResolvedAt = f.ResolvedAt
                });

            return await PaginatedList<FloatingReturnDto>.CreateAsync(projected, request.PageNumber, request.PageSize);
        }
    }
}
