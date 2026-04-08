using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Application.Warehouse.Commands.SyncWarehouseDashboard;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.AssignToWarehouse
{
    public class AssignToWarehouseCommandHandler : IRequestHandler<AssignToWarehouseCommand, AssignToWarehouseResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ISender _mediator;

        public AssignToWarehouseCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, ISender mediator)
        {
            _context = context;
            _currentUserService = currentUserService;
            _mediator = mediator;
        }

        public async Task<AssignToWarehouseResult> Handle(AssignToWarehouseCommand request, CancellationToken cancellationToken)
        {
            var warnings = new List<string>();

            var shipment = await _context.Shipments
                .Include(s => s.Project)
                .Include(s => s.Lines)
                .FirstOrDefaultAsync(x => x.Id == request.ShipmentId, cancellationToken);

            if (shipment == null)
                throw new NotFoundException("Shipment", request.ShipmentId);

            // Kıyafet operasyonu depo hazırlığa alınamaz
            if (shipment.Project.OperationType == OperationType.Clothing)
                throw new DomainException(
                    "Kıyafet operasyonu depo hazırlığa alınamaz. " +
                    "Sevkiyatı göndermek için 'Netsis'e Gönder' işlemini kullanın.");

            if (!shipment.Project.ZoneId.HasValue)
                throw new DomainException("Shipment's project does not have an assigned zone. Please assign a zone first.");

            shipment.MarkStockReserved();  // double-reserve koruması — zaten rezerveyse exception
            shipment.ChangeStatus(ShipmentStatus.AssignedToWarehouse, _currentUserService.UserId);

            // Stock: reserve ordered qty for each mapped line
            var mappedLineIds = shipment.Lines
                .Where(l => l.StockMasterId.HasValue)
                .Select(l => l.StockMasterId!.Value)
                .Distinct()
                .ToList();

            if (mappedLineIds.Count > 0)
            {
                var stocks = await _context.StockMasters
                    .Where(s => mappedLineIds.Contains(s.Id))
                    .ToListAsync(cancellationToken);

                foreach (var line in shipment.Lines.Where(l => l.StockMasterId.HasValue))
                {
                    var stock = stocks.FirstOrDefault(s => s.Id == line.StockMasterId!.Value);
                    if (stock == null) continue;

                    stock.Reserve(line.OrderedQty);

                    _context.StockTransactions.Add(new StockTransaction
                    {
                        StockMasterId = stock.Id,
                        Type = StockTransactionType.Reserve,
                        Qty = line.OrderedQty,
                        Reference = $"SHP-{shipment.Id}",
                        Date = DateTime.UtcNow,
                        Note = $"Sevkiyat #{shipment.Id} depo ataması rezervasyonu"
                    });
                }
            }

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException(
                    "Stok, aynı anda başka bir işlem tarafından güncellendi. Lütfen tekrar deneyin.");
            }

            // Sync Dashboard for the Shipment's DeliveryDate
            await _mediator.Send(new SyncWarehouseDashboardCommand(shipment.DeliveryDate), cancellationToken);

            // ── Uyarılar: revize edilmiş miktarlar ve eksik Netsis stok kodları ──
            var revisedCount = shipment.Lines
                .Count(l => l.IssOrderLine != null && l.IssOrderLine.OrderedQty != l.OrderedQty);
            if (revisedCount > 0)
                warnings.Add(
                    $"{revisedCount} kalem ISS miktarından farklı (revize edilmiş). " +
                    "Netsis aktarımında revize miktarlar kullanılacak.");

            var missingNetsis = shipment.Lines
                .Count(l => string.IsNullOrWhiteSpace(l.StockMaster?.NetsisStockCode));
            if (missingNetsis > 0)
                warnings.Add(
                    $"{missingNetsis} kalemin Netsis Stok Kodu tanımlanmamış. " +
                    "Bu kalemler Netsis'e aktarılamaz.");

            return new AssignToWarehouseResult { Warnings = warnings };
        }
    }
}
