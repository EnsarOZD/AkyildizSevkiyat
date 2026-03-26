using Akyildiz.Sevkiyat.Application.External.Netsis.Dtos;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Akyildiz.Sevkiyat.Application.Netsis.Commands.SyncIrsaliyeFromNetsis
{
    public class SyncIrsaliyeFromNetsisCommandHandler
        : IRequestHandler<SyncIrsaliyeFromNetsisCommand, SyncIrsaliyeFromNetsisResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly INetsisClient _netsisClient;
        private readonly ILogger<SyncIrsaliyeFromNetsisCommandHandler> _logger;

        public SyncIrsaliyeFromNetsisCommandHandler(
            IApplicationDbContext context,
            INetsisClient netsisClient,
            ILogger<SyncIrsaliyeFromNetsisCommandHandler> logger)
        {
            _context = context;
            _netsisClient = netsisClient;
            _logger = logger;
        }

        public async Task<SyncIrsaliyeFromNetsisResult> Handle(
            SyncIrsaliyeFromNetsisCommand request, CancellationToken cancellationToken)
        {
            // Delivered, Netsis'e aktarılmış ama irsaliye bilgisi boş olan sevkiyatlar
            var pending = await _context.Shipments
                .Include(s => s.IssOrder)
                .Where(s => s.Status == ShipmentStatus.Delivered
                         && s.NetsisTransferredAt.HasValue
                         && s.IrsaliyeNo == null)
                .ToListAsync(cancellationToken);

            var result = new SyncIrsaliyeFromNetsisResult();

            foreach (var shipment in pending)
            {
                var netsisOrderNo = shipment.IssOrder?.NetsisOrderNumber;

                if (string.IsNullOrWhiteSpace(netsisOrderNo))
                {
                    result.SkippedCount++;
                    continue;
                }

                try
                {
                    var irsaliyeler = await _netsisClient.GetIrsaliyelerAsync(
                        new NetsisIrsaliyeQuery { SiparisNo = netsisOrderNo },
                        cancellationToken);

                    // Sipariş numarasına göre ilk eşleşen irsaliyeyi al
                    var irsaliye = irsaliyeler
                        .FirstOrDefault(i => i.SiparisNo == netsisOrderNo)
                        ?? irsaliyeler.FirstOrDefault();

                    if (irsaliye == null)
                    {
                        _logger.LogDebug(
                            "SyncIrsaliye: Sevkiyat #{ShipmentId} — Netsis siparişi {NetsisOrderNo} için irsaliye bulunamadı.",
                            shipment.Id, netsisOrderNo);
                        result.NotFoundCount++;
                        continue;
                    }

                    shipment.SetIrsaliyeInfo(irsaliye.IrsaliyeNo, irsaliye.IrsaliyeTarihi);
                    result.SyncedCount++;

                    _logger.LogInformation(
                        "SyncIrsaliye: Sevkiyat #{ShipmentId} — İrsaliye {IrsaliyeNo} ({IrsaliyeTarihi:dd.MM.yyyy}) güncellendi.",
                        shipment.Id, irsaliye.IrsaliyeNo, irsaliye.IrsaliyeTarihi);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "SyncIrsaliye: Sevkiyat #{ShipmentId} — irsaliye sorgusu sırasında hata.",
                        shipment.Id);
                    result.NotFoundCount++;
                }
            }

            if (result.SyncedCount > 0)
                await _context.SaveChangesAsync(cancellationToken);

            return result;
        }
    }
}
