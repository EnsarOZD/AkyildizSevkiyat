using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Orders.Commands.CheckNetsisTransfers
{
    public record CheckNetsisTransfersCommand : IRequest<CheckNetsisTransfersResult>;

    public class CheckNetsisTransfersResult
    {
        public int Checked { get; set; }
        public int MarkedAsTransferred { get; set; }
        public string? Error { get; set; }
    }

    public class CheckNetsisTransfersCommandHandler
        : IRequestHandler<CheckNetsisTransfersCommand, CheckNetsisTransfersResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly INetsisClient _netsis;

        public CheckNetsisTransfersCommandHandler(
            IApplicationDbContext context,
            INetsisClient netsis)
        {
            _context = context;
            _netsis  = netsis;
        }

        public async Task<CheckNetsisTransfersResult> Handle(
            CheckNetsisTransfersCommand request,
            CancellationToken cancellationToken)
        {
            // Sevkiyatı Netsis'e aktarılmış ama IsTransferred bayrağı hâlâ false olan siparişleri onar
            // (SQL reset veya veri tutarsızlığından kaynaklanabilir)
            var staleOrders = await _context.IssOrders
                .Where(o => !o.IsTransferred
                    && _context.Shipments.Any(s => s.IssOrderId == o.Id && s.NetsisTransferredAt != null))
                .ToListAsync(cancellationToken);

            if (staleOrders.Count > 0)
            {
                foreach (var o in staleOrders)
                    o.IsTransferred = true;
                await _context.SaveChangesAsync(cancellationToken);
            }

            // Henüz aktarılmamış tüm siparişleri al — sevkiyatı Netsis'e gönderilmişleri hariç tut
            var orders = await _context.IssOrders
                .Where(o => !o.IsTransferred
                    && !_context.Shipments.Any(s => s.IssOrderId == o.Id && s.NetsisTransferredAt != null))
                .Select(o => new { o.Id, o.ExternalOrderNumber })
                .ToListAsync(cancellationToken);

            if (!orders.Any())
                return new CheckNetsisTransfersResult();

            // Netsis'te hangilerinin mevcut olduğunu sorgula
            var (existingInNetsis, netsisError) = await _netsis.CheckOrdersExistInNetsisAsync(
                orders.Select(o => o.ExternalOrderNumber),
                cancellationToken);

            if (!existingInNetsis.Any())
                return new CheckNetsisTransfersResult { Checked = orders.Count, Error = netsisError };

            var matchedIds = orders
                .Where(o => existingInNetsis.Contains(o.ExternalOrderNumber))
                .Select(o => o.Id)
                .ToHashSet();

            var toUpdate = await _context.IssOrders
                .Where(o => matchedIds.Contains(o.Id))
                .ToListAsync(cancellationToken);

            foreach (var order in toUpdate)
                order.IsTransferred = true;

            await _context.SaveChangesAsync(cancellationToken);

            return new CheckNetsisTransfersResult
            {
                Checked             = orders.Count,
                MarkedAsTransferred = matchedIds.Count,
                Error               = netsisError
            };
        }
    }
}
