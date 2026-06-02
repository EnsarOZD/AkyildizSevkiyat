using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.CreateManualShipment
{
    public class CreateManualShipmentCommandHandler
        : IRequestHandler<CreateManualShipmentCommand, int>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public CreateManualShipmentCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<int> Handle(
            CreateManualShipmentCommand request,
            CancellationToken cancellationToken)
        {
            var customer = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == request.CustomerId, cancellationToken);

            if (customer is null)
                throw new NotFoundException($"Müşteri #{request.CustomerId} bulunamadı.");

            if (customer.Source != ProjectSource.Manual)
                throw new DomainException(
                    "Manuel sevkiyat sadece manuel olarak eklenmiş müşteriler için oluşturulabilir. " +
                    "ISS projeleri için ISS Entegrasyon ekranını kullanın.");

            if (!customer.IsActive)
                throw new DomainException($"Pasif müşteri '{customer.Name}' için sevkiyat oluşturulamaz.");

            if (request.Lines.Count == 0)
                throw new DomainException("Sevkiyat oluşturmak için en az bir kalem gereklidir.");

            // Çoklu satırda aynı stok varsa miktarı topla
            var aggregated = request.Lines
                .GroupBy(l => l.StockMasterId)
                .Select(g => new { StockMasterId = g.Key, Qty = g.Sum(l => l.Qty) })
                .ToList();

            var stockIds = aggregated.Select(a => a.StockMasterId).ToList();
            var stocks = await _context.StockMasters
                .Where(s => stockIds.Contains(s.Id))
                .ToListAsync(cancellationToken);

            var missing = stockIds.Except(stocks.Select(s => s.Id)).ToList();
            if (missing.Count > 0)
                throw new NotFoundException(
                    "Aşağıdaki stok kartları bulunamadı: " + string.Join(", ", missing));

            // OperationType: tüm satır stokları Kıyafet kategorisindeyse Clothing, aksi halde Catering
            var operationType = stocks.Count > 0 && stocks.All(s => s.Category == StockCategory.Kiyafet)
                ? OperationType.Clothing
                : OperationType.Catering;

            var shipment = new Shipment
            {
                ProjectId = customer.Id,
                DeliveryDate = request.DeliveryDate,
                IssOrderId = null,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.FullName ?? _currentUser.Email,
                OperationType = operationType
            };

            var stockMap = stocks.ToDictionary(s => s.Id);

            foreach (var line in aggregated)
            {
                var stock = stockMap[line.StockMasterId];
                var sLine = ShipmentLine.Create(
                    issOrderLineId: null,
                    stockMasterId: stock.Id,
                    stockCode: stock.StockCode,
                    stockName: stock.StockName,
                    unit: stock.Unit,
                    orderedQty: line.Qty);

                shipment.Lines.Add(sLine);
            }

            _context.Shipments.Add(shipment);
            await _context.SaveChangesAsync(cancellationToken);

            // Depo hazırlığı gerekmeyenler doğrudan ReadyForDispatch'e taşınır
            // (Kıyafet pattern'i — Created'tan skip ile).
            if (!request.RequiresWarehousePreparation)
            {
                var skipReason = string.IsNullOrWhiteSpace(request.Notes)
                    ? "Manuel sevkiyat — depo hazırlığı atlandı."
                    : $"Manuel sevkiyat — depo hazırlığı atlandı. Not: {request.Notes}";

                shipment.ChangeStatus(ShipmentStatus.ReadyForDispatch, _currentUser.UserId, skipReason);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return shipment.Id;
        }
    }
}
