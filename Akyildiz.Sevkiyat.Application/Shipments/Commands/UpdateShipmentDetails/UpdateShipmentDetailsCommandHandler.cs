using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.SendPostponementEmail;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.UpdateShipmentDetails
{
    public class UpdateShipmentDetailsCommandHandler : IRequestHandler<UpdateShipmentDetailsCommand, UpdateShipmentDetailsResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ISender _mediator;
        private readonly ILogger<UpdateShipmentDetailsCommandHandler> _logger;

        public UpdateShipmentDetailsCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            ISender mediator,
            ILogger<UpdateShipmentDetailsCommandHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<UpdateShipmentDetailsResult> Handle(UpdateShipmentDetailsCommand request, CancellationToken cancellationToken)
        {
            // 2. Fetch Shipment
            var shipment = await _context.Shipments
                .Include(s => s.Lines)
                .FirstOrDefaultAsync(s => s.Id == request.ShipmentId, cancellationToken);

            if (shipment == null)
                throw new NotFoundException("Shipment", request.ShipmentId);

            // 3. Status Check — düzenleme yalnızca Taslak veya Sevke Hazır.
            if (shipment.Status != ShipmentStatus.Created && shipment.Status != ShipmentStatus.ReadyForDispatch)
                throw new DomainException("Sevkiyat düzenlenemez. Yalnızca 'Taslak' veya 'Sevke Hazır' durumundaki sevkiyatlar düzenlenebilir.");

            // 4. Termin tarihi değişimi — yalnızca taslak (Created) sevkiyatlarda.
            var oldDate = shipment.DeliveryDate;
            bool dateChanged = oldDate.Date != request.DeliveryDate.Date;

            if (dateChanged)
            {
                if (shipment.Status != ShipmentStatus.Created)
                    throw new DomainException("Termin tarihi yalnızca taslak sevkiyatlarda değiştirilebilir.");
                if (request.DateChangeReason == DeliveryDateChangeReason.None)
                    throw new DomainException("Termin tarihi değiştirildiğinde sebep seçilmelidir.");

                shipment.UpdateDeliveryDate(request.DeliveryDate);

                var reasonLabel = request.DateChangeReason == DeliveryDateChangeReason.Postpone ? "Erteleme" : "Diğer";
                shipment.Histories.Add(new ShipmentHistory
                {
                    ShipmentId      = shipment.Id,
                    OldStatus       = shipment.Status,
                    NewStatus       = shipment.Status,
                    ChangedByUserId = _currentUserService.UserId,
                    ChangedAt       = DateTime.UtcNow,
                    Description     = $"Termin tarihi değişti: {oldDate:dd.MM.yyyy} → {request.DeliveryDate:dd.MM.yyyy} (Sebep: {reasonLabel})"
                });
            }

            // 5. Update Lines

            // Strategy: 
            // - Updates existing lines
            // - Removes lines not in the list? (User requirement: 'Stock Card swap' -> implies changing a line or adding/removing)
            // - Adds new lines?
            // Safer approach for 'Sync':
            // If LineId matches, update.
            // If LineId is null/0, add.
            // What about deletions? Logic: If a line in DB is NOT in request, delete it?
            // Let's assume the frontend sends the COMPLETE list of desired lines.
            
            var requestLineIds = request.Lines.Where(l => l.LineId.HasValue && l.LineId.Value > 0).Select(l => l.LineId!.Value).ToList();
            var dbLineIds = shipment.Lines.Select(l => l.Id).ToList();

            // Detect Deletions
            var linesToDelete = shipment.Lines.Where(l => !requestLineIds.Contains(l.Id)).ToList();
            foreach(var lineToDelete in linesToDelete)
            {
                _context.ShipmentLines.Remove(lineToDelete);
                // shipment.Lines.Remove(lineToDelete); // EF Core tracking handles this but good to be explicit if needed
            }

            // Bulk-load StockMasters for all stock codes in request
            var requestedCodes = request.Lines
                .Select(l => l.StockCode?.Trim())
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var stockMasterMap = await _context.StockMasters
                .Where(s => requestedCodes.Contains(s.StockCode))
                .ToDictionaryAsync(s => s.StockCode, StringComparer.OrdinalIgnoreCase, cancellationToken);

            // Detect Updates & Inserts
            foreach (var lineDto in request.Lines)
            {
                stockMasterMap.TryGetValue(lineDto.StockCode ?? "", out var stockMaster);

                if (lineDto.LineId.HasValue && lineDto.LineId.Value > 0)
                {
                    // Update
                    var dbLine = shipment.Lines.FirstOrDefault(l => l.Id == lineDto.LineId.Value);
                    if (dbLine != null)
                    {
                        dbLine.UpdateStockInfo(lineDto.StockCode ?? "", lineDto.StockName ?? "", lineDto.Unit, stockMaster?.Id, updateStockMasterId: true);
                        dbLine.UpdateOrderedQty(lineDto.OrderedQty);
                    }
                }
                else
                {
                    // Insert
                    var newLine = ShipmentLine.Create(null, null, lineDto.StockCode ?? "", lineDto.StockName ?? "", lineDto.Unit, lineDto.OrderedQty);
                    if (stockMaster != null)
                        newLine.UpdateStockInfo(lineDto.StockCode ?? "", lineDto.StockName ?? "", lineDto.Unit, stockMaster.Id);
                    
                    shipment.Lines.Add(newLine);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return await FinishAsync(request, dateChanged, cancellationToken);
        }

        /// <summary>
        /// Kaydetme sonrası: termin ertelendiyse (Postpone) projeye bildirim maili gönderir.
        /// Mail hatası işlemi başarısız saymaz — kayıt zaten yapılmıştır.
        /// </summary>
        private async Task<UpdateShipmentDetailsResult> FinishAsync(
            UpdateShipmentDetailsCommand request, bool dateChanged, CancellationToken cancellationToken)
        {
            bool emailSent = false;
            string? emailError = null;

            if (dateChanged && request.DateChangeReason == DeliveryDateChangeReason.Postpone)
            {
                try
                {
                    await _mediator.Send(
                        new SendPostponementEmailCommand(request.ShipmentId, request.ExtraCc),
                        cancellationToken);
                    emailSent = true;
                }
                catch (Exception ex)
                {
                    emailError = ex.Message;
                    _logger.LogWarning(ex,
                        "Erteleme bildirimi e-postası gönderilemedi. Sevkiyat #{ShipmentId}", request.ShipmentId);
                }
            }

            return new UpdateShipmentDetailsResult(dateChanged, emailSent, emailError);
        }
    }
}
