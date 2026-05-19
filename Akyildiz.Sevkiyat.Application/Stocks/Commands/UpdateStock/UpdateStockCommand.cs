using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using System;
using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Application.Stocks.Commands.UpdateStock
{
    public record UpdateStockCommand(
        int Id,
        string StockName,
        string? Brand,
        Akyildiz.Sevkiyat.Domain.Enums.StockCategory Category,
        Akyildiz.Sevkiyat.Domain.Enums.StockUnit Unit,
        decimal? UnitPrice,
        Akyildiz.Sevkiyat.Domain.Enums.TaxRate TaxRate,
        Akyildiz.Sevkiyat.Domain.Enums.PickingType PickingType,
        decimal? MinStockQty,
        string? WarehouseLocation,
        string? NetsisStockCode = null,
        decimal? WeightKg = null,
        int PickingOrder = 0,
        string? Barcode = null
    ) : IRequest, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting" };
    }

    public class UpdateStockCommandHandler : IRequestHandler<UpdateStockCommand>
    {
        private readonly IApplicationDbContext _context;

        public UpdateStockCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(UpdateStockCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.StockMasters.FindAsync(new object[] { request.Id }, cancellationToken);

            if (entity == null)
            {
                throw new NotFoundException("Stock", request.Id);
            }

            entity.StockName = request.StockName;
            entity.Brand = request.Brand;
            entity.Category = request.Category;
            entity.Unit = request.Unit;
            entity.UnitPrice = request.UnitPrice ?? 0;
            entity.TaxRate = request.TaxRate;
            entity.PickingType = request.PickingType;
            entity.MinStockQty = request.MinStockQty;
            entity.WarehouseLocation = request.WarehouseLocation;
            entity.NetsisStockCode = string.IsNullOrWhiteSpace(request.NetsisStockCode)
                ? null
                : request.NetsisStockCode.Trim();
            entity.WeightKg = request.WeightKg;
            entity.PickingOrder = request.PickingOrder;
            entity.Barcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode.Trim();

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
