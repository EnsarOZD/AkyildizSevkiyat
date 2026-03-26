using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.StockCounts.Queries.GetStockCountDetail
{
    public record GetStockCountDetailQuery(int StockCountId) : IRequest<StockCountDetailDto>;

    public class StockCountDetailDto
    {
        public int Id { get; set; }
        public DateTime CountDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<StockCountLineDto> Lines { get; set; } = new();
    }

    public class StockCountLineDto
    {
        public int Id { get; set; }
        public int StockMasterId { get; set; }
        public string StockCode { get; set; } = string.Empty;
        public string StockName { get; set; } = string.Empty;
        public string? WarehouseLocation { get; set; }
        public decimal ExpectedQty { get; set; }
        public decimal? ActualQty { get; set; }
        public decimal? DifferenceQty { get; set; }
        public string? Note { get; set; }
    }

    public class GetStockCountDetailQueryHandler : IRequestHandler<GetStockCountDetailQuery, StockCountDetailDto>
    {
        private readonly IApplicationDbContext _context;

        public GetStockCountDetailQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<StockCountDetailDto> Handle(GetStockCountDetailQuery request, CancellationToken cancellationToken)
        {
            var stockCount = await _context.StockCounts
                .Include(c => c.Lines)
                    .ThenInclude(l => l.StockMaster)
                .FirstOrDefaultAsync(c => c.Id == request.StockCountId, cancellationToken);

            if (stockCount == null)
                throw new NotFoundException("StockCount", request.StockCountId);

            return new StockCountDetailDto
            {
                Id = stockCount.Id,
                CountDate = stockCount.CountDate,
                Status = stockCount.Status.ToString(),
                Note = stockCount.Note,
                CreatedAt = stockCount.CreatedAt,
                CompletedAt = stockCount.CompletedAt,
                Lines = stockCount.Lines
                    .OrderBy(l => l.StockMaster.StockCode)
                    .Select(l => new StockCountLineDto
                    {
                        Id = l.Id,
                        StockMasterId = l.StockMasterId,
                        StockCode = l.StockMaster.StockCode,
                        StockName = l.StockMaster.StockName,
                        WarehouseLocation = l.StockMaster.WarehouseLocation,
                        ExpectedQty = l.ExpectedQty,
                        ActualQty = l.ActualQty,
                        DifferenceQty = l.DifferenceQty,
                        Note = l.Note
                    }).ToList()
            };
        }
    }
}
