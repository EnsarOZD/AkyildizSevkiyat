using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.Netsis.Commands.ExportShipmentToNetsis;
using Akyildiz.Sevkiyat.Application.Reconciliation.Services;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.MarkReady
{
    public class MarkReadyCommandHandler : IRequestHandler<MarkReadyCommand, MarkReadyCommandResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ReconciliationGuard _guard;
        private readonly ISender _sender;
        private readonly ILogger<MarkReadyCommandHandler> _logger;

        public MarkReadyCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            ReconciliationGuard guard,
            ISender sender,
            ILogger<MarkReadyCommandHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _guard = guard;
            _sender = sender;
            _logger = logger;
        }

        public async Task<MarkReadyCommandResponse> Handle(MarkReadyCommand request, CancellationToken cancellationToken)
        {
            var response = new MarkReadyCommandResponse();

            // ── Enforcement: ISS miktar uyumsuzluğu artık süreci bloklamıyor (kullanıcı talebi) ──
            // await _guard.ThrowIfIssQtyMismatchAsync(request.ShipmentId, cancellationToken);

            var shipment = await _context.Shipments
                .Include(s => s.Project)
                .Include(s => s.Lines)
                .FirstOrDefaultAsync(x => x.Id == request.ShipmentId, cancellationToken)
                ?? throw new NotFoundException("Shipment", request.ShipmentId);

            // ── Miktar uyumsuzluğu uyarıları (non-blocking) ──────────────────────
            foreach (var line in shipment.Lines)
            {
                if (line.DeliveredQty == 0)
                    response.Warnings.Add(
                        $"{line.StockName} ({line.StockCode}): Toplanan miktar 0.");
                else if (line.DeliveredQty < line.OrderedQty)
                    response.Warnings.Add(
                        $"{line.StockName} ({line.StockCode}): Eksik toplama — " +
                        $"sipariş {line.OrderedQty}, toplanan {line.DeliveredQty}.");
                else if (line.DeliveredQty > line.OrderedQty)
                    response.Warnings.Add(
                        $"{line.StockName} ({line.StockCode}): Fazla toplama — " +
                        $"sipariş {line.OrderedQty}, toplanan {line.DeliveredQty}.");
            }

            shipment.ChangeStatus(ShipmentStatus.ReadyForDispatch, _currentUserService.UserId, request.Reason);

            await _context.SaveChangesAsync(cancellationToken);

            // ── Catering: otomatik Netsis aktarımı (non-blocking) ──
            if (shipment.OperationType == OperationType.Catering)
            {
                try
                {
                    var exportResult = await _sender.Send(
                        new ExportShipmentToNetsisCommand(shipment.Id), cancellationToken);
                    response.Warnings.AddRange(exportResult.Warnings);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Sevkiyat #{ShipmentId} Catering Netsis otomatik aktarımı başarısız", shipment.Id);
                    response.Warnings.Add(
                        "Netsis aktarımı başarısız oldu. Sevkiyatlar ekranından manuel olarak tekrar deneyebilirsiniz.");
                }
            }

            return response;
        }
    }
}
