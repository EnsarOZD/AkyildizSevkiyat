using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.VehicleReturns.Commands.ResolveVehicleReturnLine
{
    public enum VehicleReturnResolveAction
    {
        AddToStock      = 1,
        MatchToShipment = 2
    }

    public record ResolveVehicleReturnLineCommand(
        int LineId,
        VehicleReturnResolveAction Action,
        int? LinkedShipmentId = null,
        string? Note = null
    ) : IRequest;

    public class ResolveVehicleReturnLineCommandHandler : IRequestHandler<ResolveVehicleReturnLineCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public ResolveVehicleReturnLineCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task Handle(ResolveVehicleReturnLineCommand request, CancellationToken cancellationToken)
        {
            var line = await _context.VehicleReturnLines
                .FirstOrDefaultAsync(l => l.Id == request.LineId, cancellationToken);

            if (line == null)
                throw new NotFoundException("VehicleReturnLine", request.LineId);

            if (line.Status != VehicleReturnLineStatus.Pending)
                throw new DomainException("Yalnızca 'Beklemede' durumundaki satırlar çözüme kavuşturulabilir.");

            switch (request.Action)
            {
                case VehicleReturnResolveAction.AddToStock:
                    if (!line.StockMasterId.HasValue)
                        throw new DomainException("Stoğa ekleme için satırın bir stok kartıyla eşleşmiş olması gerekir.");

                    var stock = await _context.StockMasters
                        .FirstOrDefaultAsync(s => s.Id == line.StockMasterId.Value, cancellationToken);
                    if (stock == null)
                        throw new NotFoundException("StockMaster", line.StockMasterId.Value);

                    stock.Increase(line.Qty);

                    _context.StockTransactions.Add(new StockTransaction
                    {
                        StockMasterId = stock.Id,
                        Type = StockTransactionType.VehicleReturn,
                        Qty = line.Qty,
                        Reference = $"VR-{line.VehicleReturnId}-L{line.Id}",
                        Date = DateTime.UtcNow,
                        Note = string.IsNullOrWhiteSpace(request.Note)
                            ? $"Araç iade #{line.VehicleReturnId} stoğa eklendi"
                            : request.Note
                    });

                    line.Status = VehicleReturnLineStatus.AddedToStock;
                    break;

                case VehicleReturnResolveAction.MatchToShipment:
                    if (!request.LinkedShipmentId.HasValue)
                        throw new DomainException("Sevkiyata eşleştirme için sevkiyat ID'si gereklidir.");

                    var shipmentExists = await _context.Shipments
                        .AnyAsync(s => s.Id == request.LinkedShipmentId.Value, cancellationToken);
                    if (!shipmentExists)
                        throw new NotFoundException("Shipment", request.LinkedShipmentId.Value);

                    line.LinkedShipmentId = request.LinkedShipmentId;
                    line.Status = VehicleReturnLineStatus.MatchedToShipment;
                    break;

                default:
                    throw new DomainException($"Geçersiz çözüm eylemi: {request.Action}");
            }

            if (!string.IsNullOrWhiteSpace(request.Note))
                line.Note = (line.Note + "\n" + request.Note).Trim();

            line.ResolvedAt = DateTime.UtcNow;
            line.ResolvedByUserId = _currentUserService.UserId;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
