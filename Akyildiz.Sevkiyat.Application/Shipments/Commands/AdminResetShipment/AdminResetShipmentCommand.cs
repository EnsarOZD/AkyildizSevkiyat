using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.AdminResetShipment
{
    /// <summary>
    /// Admin seviyesinde sevkiyatı sıfırlar: Netsis transfer verisi temizlenir,
    /// IssOrder serbest bırakılır, sevkiyat Created statüsüne döner.
    /// Yalnızca ReturnedToWarehouse, Delivered ve Cancelled durumları için geçerlidir.
    /// </summary>
    public record AdminResetShipmentCommand(int ShipmentId, string Reason) : IRequest<Unit>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager" };
    }

    public class AdminResetShipmentCommandHandler : IRequestHandler<AdminResetShipmentCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public AdminResetShipmentCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Unit> Handle(AdminResetShipmentCommand request, CancellationToken cancellationToken)
        {
            var shipment = await _context.Shipments
                .Include(s => s.Lines)
                .Include(s => s.IssOrder)
                .FirstOrDefaultAsync(s => s.Id == request.ShipmentId, cancellationToken)
                ?? throw new NotFoundException("Shipment", request.ShipmentId);

            var resettableStatuses = new[]
            {
                ShipmentStatus.ReturnedToWarehouse,
                ShipmentStatus.Delivered,
                ShipmentStatus.Cancelled,
            };

            if (!resettableStatuses.Contains(shipment.Status))
                throw new DomainException(
                    $"Bu işlem yalnızca Depoya İade, Teslim Edildi veya İptal durumundaki sevkiyatlara uygulanabilir. " +
                    $"Mevcut durum: {shipment.Status}");

            // 1. Netsis transfer verilerini temizle
            shipment.RevertNetsisTransfer();

            // 2. IssOrder'ı serbest bırak
            if (shipment.IssOrder != null)
            {
                shipment.IssOrder.IsTransferred = false;
                shipment.IssOrder.NetsisOrderNumber = null;
            }

            // 3. Picking verilerini sıfırla
            foreach (var line in shipment.Lines)
                line.ResetPickingData();

            // 4. İade verilerini sıfırla
            foreach (var line in shipment.Lines.Where(l => l.ReturnedQty.HasValue))
                line.ClearReturnData();

            // 5. Stok rezervasyonu serbest bırak
            if (shipment.StockReserved)
                shipment.MarkStockReleased();

            // 6. Araç/şoför bilgilerini temizle
            shipment.ClearVehicleAssignment();

            // 7. Created statüsüne döndür
            shipment.ChangeStatus(
                ShipmentStatus.Created,
                _currentUserService.UserId,
                $"Admin sıfırlaması — {request.Reason}");

            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
