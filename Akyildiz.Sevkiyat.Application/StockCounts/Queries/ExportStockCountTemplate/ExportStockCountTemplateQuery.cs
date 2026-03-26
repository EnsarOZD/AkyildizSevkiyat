using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces.Models;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.StockCounts.Queries.ExportStockCountTemplate
{
    public class ExportStockCountTemplateQuery : IRequest<byte[]>
    {
        public int StockCountId { get; set; }
        
        public ExportStockCountTemplateQuery(int stockCountId)
        {
            StockCountId = stockCountId;
        }
    }

    public class ExportStockCountTemplateQueryHandler 
        : IRequestHandler<ExportStockCountTemplateQuery, byte[]>
    {
        private readonly IApplicationDbContext _context;
        private readonly IStockCountExcelService _excelService;

        public ExportStockCountTemplateQueryHandler(
            IApplicationDbContext context, 
            IStockCountExcelService excelService)
        {
            _context = context;
            _excelService = excelService;
        }

        public async Task<byte[]> Handle(ExportStockCountTemplateQuery request, CancellationToken cancellationToken)
        {
            var stockCount = await _context.StockCounts
                .Include(sc => sc.Lines)
                .ThenInclude(l => l.StockMaster)
                .FirstOrDefaultAsync(sc => sc.Id == request.StockCountId, cancellationToken);

            if (stockCount == null)
                throw new NotFoundException("Sayım kaydı bulunamadı.");

            var dtos = stockCount.Lines.Select(l => new StockCountTemplateRowDto
            {
                LineId = l.Id,
                StockCode = l.StockMaster.StockCode,
                StockName = l.StockMaster.StockName,
                ExpectedQty = l.ExpectedQty,
                ActualQty = l.ActualQty
            }).OrderBy(l => l.StockName).ToList();

            var excelData = _excelService.GenerateTemplate(dtos);
            
            return excelData;
        }
    }
}
