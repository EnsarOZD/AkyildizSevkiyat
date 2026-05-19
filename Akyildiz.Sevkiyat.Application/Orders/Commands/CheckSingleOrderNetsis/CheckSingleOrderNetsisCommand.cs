using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Akyildiz.Sevkiyat.Application.Orders.Commands.CheckSingleOrderNetsis
{
    public record CheckSingleOrderNetsisCommand(string OrderNumber) : IRequest<CheckSingleOrderNetsisResult>;

    public class CheckSingleOrderNetsisResult
    {
        public bool Found { get; set; }
        public bool WasTransferred { get; set; }
        public bool ExistsInNetsis { get; set; }
        public bool Reverted { get; set; }
        public string? Message { get; set; }
        public string? ExternalOrderNumber { get; set; }
    }

    public class CheckSingleOrderNetsisCommandHandler
        : IRequestHandler<CheckSingleOrderNetsisCommand, CheckSingleOrderNetsisResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly INetsisClient _netsis;
        private readonly ILogger<CheckSingleOrderNetsisCommandHandler> _logger;

        public CheckSingleOrderNetsisCommandHandler(
            IApplicationDbContext context,
            INetsisClient netsis,
            ILogger<CheckSingleOrderNetsisCommandHandler> logger)
        {
            _context = context;
            _netsis  = netsis;
            _logger  = logger;
        }

        public async Task<CheckSingleOrderNetsisResult> Handle(
            CheckSingleOrderNetsisCommand request,
            CancellationToken cancellationToken)
        {
            var number = request.OrderNumber.Trim();

            // ISS ExternalOrderNumber veya Netsis NetsisOrderNumber ile ara
            var order = await _context.IssOrders
                .FirstOrDefaultAsync(o => o.ExternalOrderNumber == number
                                       || o.NetsisOrderNumber == number,
                    cancellationToken);

            if (order == null)
                return new CheckSingleOrderNetsisResult
                {
                    Found   = false,
                    Message = $"'{number}' numaralı sipariş sistemde bulunamadı.",
                };

            var result = new CheckSingleOrderNetsisResult
            {
                Found               = true,
                WasTransferred      = order.IsTransferred,
                ExternalOrderNumber = order.ExternalOrderNumber,
            };

            if (!order.IsTransferred)
            {
                result.Message = $"Sipariş zaten aktarılmamış durumda (IsTransferred=false). Netsis kontrolüne gerek yok.";
                return result;
            }

            // Netsis'te var mı?
            var (existing, error) = await _netsis.CheckOrdersExistInNetsisAsync(
                new[] { order.ExternalOrderNumber }, cancellationToken);

            if (error != null)
            {
                result.Message = $"Netsis bağlantı hatası: {error}";
                return result;
            }

            result.ExistsInNetsis = existing.Contains(order.ExternalOrderNumber);

            if (result.ExistsInNetsis)
            {
                result.Message = $"Sipariş Netsis'te mevcut. Herhangi bir değişiklik yapılmadı.";
                return result;
            }

            // Netsis'te yok → geri al
            order.IsTransferred      = false;
            order.NetsisOrderNumber  = null;

            var shipments = await _context.Shipments
                .Where(s => s.IssOrderId == order.Id && s.NetsisTransferredAt != null)
                .ToListAsync(cancellationToken);

            foreach (var s in shipments)
                s.RevertNetsisTransfer();

            await _context.SaveChangesAsync(cancellationToken);

            result.Reverted = true;
            result.Message  = $"Sipariş Netsis'te bulunamadı. IsTransferred sıfırlandı" +
                              (shipments.Count > 0 ? $" ve {shipments.Count} sevkiyatın aktarım bilgisi temizlendi." : ".");

            _logger.LogWarning(
                "CheckSingleOrderNetsis: '{Number}' → '{Ext}' Netsis'te yok; IssOrder #{Id} ve {Cnt} sevkiyat sıfırlandı.",
                number, order.ExternalOrderNumber, order.Id, shipments.Count);

            return result;
        }
    }
}
