using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Shipments.Queries.GetShipmentDetail
{
    public class GetShipmentDetailQueryHandler : IRequestHandler<GetShipmentDetailQuery, ShipmentDetailDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetShipmentDetailQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<ShipmentDetailDto> Handle(GetShipmentDetailQuery request, CancellationToken cancellationToken)
        {
            var shipment = await _context.Shipments
                .Include(s => s.Project)
                    .ThenInclude(p => p.Zone)
                .Include(s => s.IssOrder) // Added
                .Include(s => s.Lines)
                .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

            if (shipment == null)
            {
                throw new NotFoundException("Shipment", request.Id);
            }

            // Driver ownership check: Driver can only view their own shipments
            if (_currentUserService.Role == UserRole.Driver)
            {
                var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == _currentUserService.UserId, cancellationToken)
                    ?? throw new ForbiddenException("Kullanıcıya tanımlı bir şoför kaydı bulunamadı.");
                if (shipment.AssignedDriverId != driver.Id)
                    throw new ForbiddenException("Bu sevkiyata erişim yetkiniz yok.");
            }

            // ... (rest of logic)

            // ... (inside DTO mapping)


            if (shipment == null)
            {
                throw new NotFoundException("Shipment", request.Id);
            }

            // Fetch history separately
            // 1. Collect all unique StockCodes from lines
            var externalCodes = shipment.Lines.Select(l => l.StockCode).Distinct().ToList();

            // 2. Fetch Mappings
            var mappings = await _context.StockMappings
                .Where(m => externalCodes.Contains(m.ExternalStockCode) && m.MatchStatus == MatchStatus.Mapped)
                .Select(m => new { m.ExternalStockCode, m.LocalStockId })
                .ToListAsync(cancellationToken);

            // 3. Fetch Local Stocks for the found mappings
            var localStockIds = mappings
                .Where(m => m.LocalStockId.HasValue)
                .Select(m => m.LocalStockId!.Value)
                .Distinct()
                .ToList();

            var localStocks = await _context.StockMasters
                .Where(s => localStockIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s, cancellationToken);
            
            // Dictionary for fast lookup: ExternalCode -> LocalStock
            var definitionMap = new Dictionary<string, StockMaster>();
            foreach (var m in mappings)
            {
                if (m.LocalStockId.HasValue && localStocks.TryGetValue(m.LocalStockId.Value, out var stock))
                {
                    definitionMap[m.ExternalStockCode] = stock;
                }
            }

            // Fetch print logs
            var printLogs = await _context.ShipmentPrintLogs
                .Where(l => l.ShipmentId == request.Id)
                .OrderByDescending(l => l.PrintedAt)
                .ToListAsync(cancellationToken);

            // Fetch history
            var history = await _context.ShipmentHistories
                .Where(h => h.ShipmentId == request.Id)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync(cancellationToken);
            
            // Get user names for history
            var userIds = history.Select(h => h.ChangedByUserId).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
            var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FirstName + " " + u.LastName, cancellationToken);

            var zoneName = shipment.Project.Zone?.Name ?? "Tanımsız";
            var zoneOrder = shipment.Project.Zone?.Order ?? 9999;

            return new ShipmentDetailDto
            {
                Id = shipment.Id,
                ProjectId = shipment.ProjectId,
                ProjectCode = shipment.Project.Code,
                ProjectName = shipment.Project.Name,
                ProjectAddress = shipment.Project.Address,
                ZoneId = shipment.Project.ZoneId,
                ZoneName = shipment.Project.Zone?.Name,
                Status = shipment.Status.ToString(),
                DeliveryDate = shipment.DeliveryDate,
                DriverName = shipment.AssignedDriverName,
                PlateNumber = shipment.AssignedPlateNumber,

                // Netsis / İrsaliye
                IrsaliyeNo = shipment.IrsaliyeNo,
                IrsaliyeDate = shipment.IrsaliyeDate == DateOnly.MinValue ? null : shipment.IrsaliyeDate,
                NetsisTransferredAt = shipment.NetsisTransferredAt,

                // Delivery Proof
                DeliveredAt = shipment.DeliveredAt,
                DeliveryNote = shipment.DeliveryNote,
                DeliveryRecipient = shipment.DeliveryRecipient,
                DeliveryPhotoBase64 = shipment.DeliveryPhotoBase64,

                OperationType      = shipment.OperationType == Domain.Enums.OperationType.Clothing ? "Kıyafet" : "Catering",
                OperationTypeValue = (int)shipment.OperationType,

                // Mapped Fields
                ExternalOrderNumber = shipment.IssOrder?.ExternalOrderNumber,
                TalepNo = shipment.TalepNo ?? shipment.IssOrder?.TalepNo,
                TeslimAlacakKisiler = shipment.IssOrder?.TeslimAlacakKisiler,
                TeslimAlacakTelefon = shipment.IssOrder?.TeslimAlacakTelefonNumaralari,
                YoneticiMail = shipment.IssOrder?.YoneticiMailAdresleri,
                Aciklama = shipment.IssOrder?.Aciklama,

                Lines = shipment.Lines.Select(l => 
                {
                    // Lookup local stock
                    var hasLocal = definitionMap.TryGetValue(l.StockCode, out var localStock);

                    return new ShipmentLineDetailDto
                    {
                        Id = l.Id,
                        StockCode = l.StockCode, // Keep original ISS code here if needed, or maybe we swap? 
                                                 // Frontend wants to show "LocalStockCode".
                                                 // DTO has "LocalStockCode" property now.
                        StockName = hasLocal ? localStock!.StockName : l.StockName,
                        
                        LocalStockCode = hasLocal ? localStock!.StockCode : l.StockCode, // Fallback to ISS if no map
                        Unit = hasLocal ? localStock!.Unit.ToString() : l.Unit.ToString(),
                        StockQty = null, // Placeholder

                        OrderedQty = l.OrderedQty,
                        DeliveredQty = l.DeliveredQty,
                        DifferenceReason = l.DifferenceReason,
                        Note = l.Note,
                        ZoneName = zoneName,
                        ZoneOrder = zoneOrder
                    };
                }).OrderBy(l => l.LocalStockCode).ToList(), // Sort by Local Code now
                History = history.Select(h => new ShipmentHistoryDto
                {
                    OldStatus = h.OldStatus.ToString(),
                    NewStatus = h.NewStatus.ToString(),
                    ChangedAt = h.ChangedAt,
                    ChangedBy = h.ChangedByUserId.HasValue && users.ContainsKey(h.ChangedByUserId.Value)
                        ? users[h.ChangedByUserId.Value]
                        : (h.ChangedByUserId.HasValue ? h.ChangedByUserId.Value.ToString() : "System"),
                    Description = h.Description
                }).ToList(),
                PrintLogs = printLogs.Select(l => new ShipmentPrintLogDto
                {
                    Id = l.Id,
                    PrintedAt = l.PrintedAt,
                    PrintedByName = l.PrintedByName,
                }).ToList()
            };
        }
    }
}
