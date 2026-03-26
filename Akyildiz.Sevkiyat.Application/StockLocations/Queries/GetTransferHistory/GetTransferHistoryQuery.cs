using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.StockLocations.Queries.GetTransferHistory
{
    public record GetTransferHistoryQuery(
        int? StockMasterId = null,
        int? LocationId    = null,
        int  Page          = 1,
        int  PageSize      = 50
    ) : IRequest<GetTransferHistoryResult>;

    public record TransferHistoryDto(
        int      Id,
        string   StockCode,
        string   StockName,
        string   FromLocationCode,
        string   ToLocationCode,
        decimal  Qty,
        string?  Note,
        string?  TransferredBy,
        DateTime TransferredAt
    );

    public record GetTransferHistoryResult(
        IReadOnlyList<TransferHistoryDto> Items,
        int TotalCount
    );

    public class GetTransferHistoryQueryHandler
        : IRequestHandler<GetTransferHistoryQuery, GetTransferHistoryResult>
    {
        private readonly IApplicationDbContext _context;

        public GetTransferHistoryQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GetTransferHistoryResult> Handle(
            GetTransferHistoryQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.LocationTransfers
                .Include(t => t.StockMaster)
                .Include(t => t.FromLocation)
                .Include(t => t.ToLocation)
                .AsQueryable();

            if (request.StockMasterId.HasValue)
                query = query.Where(t => t.StockMasterId == request.StockMasterId.Value);

            if (request.LocationId.HasValue)
                query = query.Where(t =>
                    t.FromLocationId == request.LocationId.Value ||
                    t.ToLocationId   == request.LocationId.Value);

            var total = await query.CountAsync(cancellationToken);

            var userIds = await query
                .Where(t => t.TransferredByUserId != null)
                .Select(t => t.TransferredByUserId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}".Trim(), cancellationToken);

            var items = await query
                .OrderByDescending(t => t.TransferredAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(t => new
                {
                    t.Id,
                    StockCode        = t.StockMaster.StockCode,
                    StockName        = t.StockMaster.StockName,
                    FromLocationCode = t.FromLocation.Code,
                    ToLocationCode   = t.ToLocation.Code,
                    t.Qty,
                    t.Note,
                    t.TransferredByUserId,
                    t.TransferredAt,
                })
                .ToListAsync(cancellationToken);

            var result = items.Select(t => new TransferHistoryDto(
                t.Id,
                t.StockCode,
                t.StockName,
                t.FromLocationCode,
                t.ToLocationCode,
                t.Qty,
                t.Note,
                t.TransferredByUserId.HasValue && users.TryGetValue(t.TransferredByUserId.Value, out var name) ? name : null,
                t.TransferredAt
            )).ToList();

            return new GetTransferHistoryResult(result, total);
        }
    }
}
