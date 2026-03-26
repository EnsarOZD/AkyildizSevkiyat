using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.RevertToDraft
{
    public record RevertToDraftCommand(int ShipmentId, string Reason) : IRequest<Unit>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager" };
    }

    public class RevertToDraftCommandHandler : IRequestHandler<RevertToDraftCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public RevertToDraftCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Unit> Handle(RevertToDraftCommand request, CancellationToken cancellationToken)
        {
            var shipment = await _context.Shipments
                .Include(s => s.Lines)
                .FirstOrDefaultAsync(s => s.Id == request.ShipmentId, cancellationToken);

            if (shipment == null) throw new NotFoundException("Shipment", request.ShipmentId);

            // Allow revert only from AssignedToWarehouse or Picking
            if (shipment.Status != ShipmentStatus.AssignedToWarehouse && shipment.Status != ShipmentStatus.Picking)
                throw new DomainException("Only shipments in 'AssignedToWarehouse' or 'Picking' status can be reverted to Draft.");

            var previousStatus = shipment.Status;

            // Revert Status (Aggregate handles history logging now)
            shipment.ChangeStatus(ShipmentStatus.Created, _currentUserService.UserId, request.Reason);

            // Stock: release reservation that was placed at AssignToWarehouse
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

                    stock.ReleaseReservation(line.OrderedQty);

                    _context.StockTransactions.Add(new StockTransaction
                    {
                        StockMasterId = stock.Id,
                        Type = StockTransactionType.ReleaseReserve,
                        Qty = -line.OrderedQty,
                        Reference = $"SHP-{shipment.Id}",
                        Date = DateTime.UtcNow,
                        Note = $"Sevkiyat #{shipment.Id} taslağa geri alındı — rezervasyon serbest bırakıldı"
                    });
                }
            }

            // StockReserved flag'ini her zaman serbest bırak (mapped satır olmasa da)
            if (shipment.StockReserved)
                shipment.MarkStockReleased();

            // Picking verilerini temizle — sonraki turda kirli veri kalmasın.
            var dirtyLines = shipment.Lines.Where(l => l.DeliveredQty != 0).ToList();
            foreach (var line in dirtyLines)
                line.ResetPickingData();

            if (dirtyLines.Count > 0)
            {
                _context.ShipmentHistories.Add(new ShipmentHistory
                {
                    ShipmentId = shipment.Id,
                    OldStatus  = previousStatus,
                    NewStatus  = ShipmentStatus.Created,
                    ChangedAt  = DateTime.UtcNow,
                    ChangedByUserId = _currentUserService.UserId,
                    Description = $"Picking verileri temizlendi: {dirtyLines.Count} satır, " +
                                  $"toplam {dirtyLines.Sum(l => l.OrderedQty)} adet sıfırlandı."
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
