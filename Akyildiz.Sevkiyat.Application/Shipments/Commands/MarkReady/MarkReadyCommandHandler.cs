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

            // ── Enforcement: ISS miktar uyumsuzluğu varsa hazır işaretlenemez ──
            await _guard.ThrowIfIssQtyMismatchAsync(request.ShipmentId, cancellationToken);

            var shipment = await _context.Shipments
                .Include(s => s.Project)
                .FirstOrDefaultAsync(x => x.Id == request.ShipmentId, cancellationToken)
                ?? throw new NotFoundException("Shipment", request.ShipmentId);

            shipment.ChangeStatus(ShipmentStatus.ReadyForDispatch, _currentUserService.UserId, request.Reason);

            await _context.SaveChangesAsync(cancellationToken);

            // ── Catering: otomatik Netsis aktarımı (non-blocking) ──
            if (shipment.Project.OperationType == OperationType.Catering)
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
