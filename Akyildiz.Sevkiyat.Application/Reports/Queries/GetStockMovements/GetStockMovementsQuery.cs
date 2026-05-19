using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Reports.Queries.GetStockMovements
{
    public record GetStockMovementsQuery(
        DateTime  DateFrom,
        DateTime  DateTo,
        string?   StockSearch   = null,
        int?      Type          = null,
        int       Page          = 1,
        int       PageSize      = 50
    ) : IRequest<GetStockMovementsResult>;

    public record StockMovementRow(
        int      Id,
        DateTime Date,
        string   StockCode,
        string   StockName,
        string   Unit,
        string   TransactionType,
        int      TransactionTypeId,
        decimal  Qty,
        string?  Reference,
        string?  Note
    );

    public record GetStockMovementsResult(
        IReadOnlyList<StockMovementRow> Items,
        int TotalCount,
        int Page,
        int PageSize
    );

    public class GetStockMovementsQueryHandler
        : IRequestHandler<GetStockMovementsQuery, GetStockMovementsResult>
    {
        private static readonly Dictionary<StockTransactionType, string> TypeLabels = new()
        {
            { StockTransactionType.GoodsIn,           "Mal Girişi"          },
            { StockTransactionType.ShipmentOut,        "Sevkiyat Çıkışı"    },
            { StockTransactionType.ManualAdjust,       "Manuel Düzeltme"    },
            { StockTransactionType.Reserve,            "Rezervasyon"        },
            { StockTransactionType.ReleaseReserve,     "Rezervasyon İptali" },
            { StockTransactionType.VehicleReturn,      "Araç İadesi"        },
            { StockTransactionType.GoodsInCorrection,  "Giriş Düzeltmesi"  },
        };

        private readonly IApplicationDbContext _context;

        public GetStockMovementsQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<GetStockMovementsResult> Handle(
            GetStockMovementsQuery request, CancellationToken cancellationToken)
        {
            var dateFrom = request.DateFrom.Date;
            var dateTo   = request.DateTo.Date.AddDays(1);

            var query = _context.StockTransactions
                .Where(t => t.Date >= dateFrom && t.Date < dateTo);

            if (request.Type.HasValue)
                query = query.Where(t => (int)t.Type == request.Type.Value);

            if (!string.IsNullOrWhiteSpace(request.StockSearch))
            {
                var s = request.StockSearch.Trim().ToLower();
                query = query.Where(t =>
                    t.StockMaster.StockCode.ToLower().Contains(s) ||
                    t.StockMaster.StockName.ToLower().Contains(s));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.Id)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(t => new StockMovementRow(
                    t.Id,
                    t.Date,
                    t.StockMaster.StockCode,
                    t.StockMaster.StockName,
                    t.StockMaster.Unit.ToString(),
                    t.Type.ToString(),
                    (int)t.Type,
                    t.Qty,
                    t.Reference,
                    t.Note
                ))
                .ToListAsync(cancellationToken);

            // Localize TransactionType label after projection
            var localized = items.Select(r => r with
            {
                TransactionType = TypeLabels.TryGetValue((StockTransactionType)r.TransactionTypeId, out var lbl) ? lbl : r.TransactionType
            }).ToList();

            return new GetStockMovementsResult(localized, totalCount, request.Page, request.PageSize);
        }
    }
}
