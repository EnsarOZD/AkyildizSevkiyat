using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.FloatingReturns.Commands.ResolveFloatingReturn
{
    public class ResolveFloatingReturnCommandHandler : IRequestHandler<ResolveFloatingReturnCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public ResolveFloatingReturnCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Unit> Handle(ResolveFloatingReturnCommand request, CancellationToken cancellationToken)
        {
            var floatingReturn = await _context.FloatingReturns
                .FirstOrDefaultAsync(f => f.Id == request.FloatingReturnId, cancellationToken);

            if (floatingReturn == null)
                throw new NotFoundException("FloatingReturn", request.FloatingReturnId);

            if (floatingReturn.Status != FloatingReturnStatus.Pending)
                throw new DomainException("Yalnızca 'Beklemede' durumundaki kayıtlar çözüme kavuşturulabilir.");

            switch (request.Action)
            {
                case ResolveAction.MatchToShipment:
                    if (!request.LinkedShipmentId.HasValue)
                        throw new DomainException("Sevkiyata eşleştirme için LinkedShipmentId gereklidir.");

                    var shipmentExists = await _context.Shipments
                        .AnyAsync(s => s.Id == request.LinkedShipmentId.Value, cancellationToken);
                    if (!shipmentExists)
                        throw new NotFoundException("Shipment", request.LinkedShipmentId.Value);

                    floatingReturn.LinkedShipmentId = request.LinkedShipmentId;
                    floatingReturn.Status = FloatingReturnStatus.MatchedToShipment;
                    break;

                case ResolveAction.AddToStock:
                    if (!floatingReturn.StockMasterId.HasValue)
                        throw new DomainException("Stoğa ekleme için floating return'ün bir StockMaster ile eşleşmiş olması gerekir.");

                    var stock = await _context.StockMasters
                        .FirstOrDefaultAsync(s => s.Id == floatingReturn.StockMasterId.Value, cancellationToken);
                    if (stock == null)
                        throw new NotFoundException("StockMaster", floatingReturn.StockMasterId.Value);

                    stock.Increase(floatingReturn.Qty);

                    _context.StockTransactions.Add(new StockTransaction
                    {
                        StockMasterId = stock.Id,
                        Type = StockTransactionType.VehicleReturn,
                        Qty = floatingReturn.Qty,
                        Reference = $"FR-{floatingReturn.Id}",
                        Date = DateTime.UtcNow,
                        Note = $"Floating Return #{floatingReturn.Id} stoğa eklendi"
                    });

                    floatingReturn.Status = FloatingReturnStatus.AddedToStock;
                    break;

                case ResolveAction.WriteOff:
                    floatingReturn.Status = FloatingReturnStatus.WrittenOff;
                    break;

                default:
                    throw new DomainException($"Geçersiz çözüm eylemi: {request.Action}");
            }

            if (!string.IsNullOrWhiteSpace(request.Note))
                floatingReturn.Note = (floatingReturn.Note + "\n" + request.Note).Trim();

            floatingReturn.ResolvedAt = DateTime.UtcNow;
            floatingReturn.ResolvedByUserId = _currentUserService.UserId;

            await _context.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
