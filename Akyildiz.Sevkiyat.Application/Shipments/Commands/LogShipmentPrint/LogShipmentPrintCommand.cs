using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.LogShipmentPrint
{
    public record LogShipmentPrintCommand(int ShipmentId) : IRequest<LogShipmentPrintResult>;

    public record LogShipmentPrintResult(
        int LogId,
        DateTime PrintedAt,
        string PrintedByName,
        bool WasPreviouslyPrinted,
        int PreviousPrintCount
    );

    public class LogShipmentPrintCommandHandler : IRequestHandler<LogShipmentPrintCommand, LogShipmentPrintResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public LogShipmentPrintCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<LogShipmentPrintResult> Handle(LogShipmentPrintCommand request, CancellationToken cancellationToken)
        {
            var shipmentExists = await _context.Shipments.AnyAsync(s => s.Id == request.ShipmentId, cancellationToken);
            if (!shipmentExists)
                throw new NotFoundException("Shipment", request.ShipmentId);

            // Resolve user display name
            string printedByName = "Bilinmiyor";
            if (_currentUser.UserId.HasValue)
            {
                var user = await _context.Users
                    .Where(u => u.Id == _currentUser.UserId.Value)
                    .Select(u => new { u.FirstName, u.LastName })
                    .FirstOrDefaultAsync(cancellationToken);
                if (user != null)
                    printedByName = $"{user.FirstName} {user.LastName}".Trim();
            }

            // Count existing prints before adding the new one
            var previousPrintCount = await _context.ShipmentPrintLogs
                .CountAsync(l => l.ShipmentId == request.ShipmentId, cancellationToken);

            var log = new ShipmentPrintLog
            {
                ShipmentId = request.ShipmentId,
                PrintedAt = DateTime.UtcNow,
                PrintedByUserId = _currentUser.UserId,
                PrintedByName = printedByName,
            };

            _context.ShipmentPrintLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);

            return new LogShipmentPrintResult(
                LogId: log.Id,
                PrintedAt: log.PrintedAt,
                PrintedByName: printedByName,
                WasPreviouslyPrinted: previousPrintCount > 0,
                PreviousPrintCount: previousPrintCount
            );
        }
    }
}
