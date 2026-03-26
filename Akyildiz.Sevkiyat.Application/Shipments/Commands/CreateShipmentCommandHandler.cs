using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.CreateShipment;
using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands
{
    public class CreateShipmentCommandHandler
        : IRequestHandler<CreateShipmentCommand, int>
    {
        private readonly IApplicationDbContext _context;

        public CreateShipmentCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(
            CreateShipmentCommand request,
            CancellationToken cancellationToken)
        {
            // 1) İlgili ISS siparişini satırları ile birlikte bul
            var order = await _context.IssOrders
                .Include(o => o.Project)
                .Include(o => o.Lines)
                .FirstOrDefaultAsync(o => o.Id == request.IssOrderId, cancellationToken);

            if (order == null)
                throw new NotFoundException("ISS siparişi bulunamadı.");

            // Check if already transferred
            if (order.IsTransferred)
            {
                throw new ConflictException("Bu sipariş zaten bir sevkiyata dönüştürülmüş.");
            }

            // Double check existing shipments just in case
            var existingShipment = await _context.Shipments
                .AnyAsync(s => s.IssOrderId == request.IssOrderId, cancellationToken);

            if (existingShipment)
            {
                throw new ConflictException("Bu sipariş zaten bir sevkiyata dönüştürülmüş.");
            }

            if (order.ImportStatus == ImportStatus.NeedsMapping)
                throw new DomainException("Stok eşleştirmesi tamamlanmadan sevkiyat oluşturulamaz.");

            if (!order.IsActive)
                throw new DomainException("Pasif sipariş sevkiyata dönüştürülemez.");

            if (!order.Lines.Any())
                throw new DomainException("Siparişin satırı bulunmuyor, sevkiyat oluşturulamaz.");

            // Load stock mapping lookup to populate StockMasterId on shipment lines
            var stockCodes = order.Lines.Select(l => l.StockCode).ToList();
            var mappingLookup = await _context.StockMappings
                .Where(m => stockCodes.Contains(m.ExternalStockCode)
                            && m.MatchStatus == MatchStatus.Mapped
                            && m.LocalStockId != null)
                .ToDictionaryAsync(m => m.ExternalStockCode, m => m.LocalStockId, cancellationToken);

            // 2) Yeni Shipment oluştur
            var shipment = new Shipment
            {
                ProjectId = order.ProjectId,
                DeliveryDate = order.DeliveryDate,
                IssOrderId = order.Id,
                CreatedAt = DateTime.UtcNow,
                TalepNo = order.TalepNo // Transfer TalepNo if available
            };

            // 3) Her order line için bir shipment line ekle
            foreach (var line in order.Lines.OrderBy(l => l.LineNumber))
            {
                var sLine = ShipmentLine.Create(
                    line.Id,
                    mappingLookup.TryGetValue(line.StockCode, out var sid) ? sid : null,
                    line.StockCode,
                    line.StockName,
                    line.Unit,
                    line.OrderedQty
                );

                shipment.Lines.Add(sLine);
            }

            // 4) Kaydet ve İşaretle
            _context.Shipments.Add(shipment);
            order.IsTransferred = true; // Mark as transferred
            
            await _context.SaveChangesAsync(cancellationToken);

            return shipment.Id;
        }
    }
}
