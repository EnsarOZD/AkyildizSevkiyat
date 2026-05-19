using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.External.YurtiKargo;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Akyildiz.Sevkiyat.Domain.Entities;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.DispatchZoneAsCargo
{
    public record YkDesiLine
    {
        public int Count { get; init; }
        public decimal Desi { get; init; }
    }

    public record DispatchZoneAsCargoCommand : IRequest<Unit>, IRequireRoles
    {
        public int ZonePreparationId { get; init; }
        public CargoProvider CargoProvider { get; init; }
        public string? CargoTrackingNumber { get; init; }
        public List<YkDesiLine>? YkDesiLines { get; init; }

        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting", "Warehouse" };
    }

    public class DispatchZoneAsCargoCommandValidator : AbstractValidator<DispatchZoneAsCargoCommand>
    {
        public DispatchZoneAsCargoCommandValidator()
        {
            RuleFor(x => x.ZonePreparationId).GreaterThan(0);
            RuleFor(x => x.CargoProvider).IsInEnum();
        }
    }

    public class DispatchZoneAsCargoCommandHandler : IRequestHandler<DispatchZoneAsCargoCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IYurtiKargoClient? _ykClient;
        private readonly ILogger<DispatchZoneAsCargoCommandHandler> _log;

        public DispatchZoneAsCargoCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            ILogger<DispatchZoneAsCargoCommandHandler> log,
            IYurtiKargoClient? ykClient = null)
        {
            _context = context;
            _currentUserService = currentUserService;
            _log = log;
            _ykClient = ykClient;
        }

        public async Task<Unit> Handle(DispatchZoneAsCargoCommand request, CancellationToken cancellationToken)
        {
            var zp = await _context.ZonePreparations
                .FirstOrDefaultAsync(z => z.Id == request.ZonePreparationId, cancellationToken)
                ?? throw new NotFoundException("ZonePreparation", request.ZonePreparationId);

            if (zp.Status != ZonePreparationStatus.ReadyForDriverInfo)
                throw new DomainException("Kargo gönderimi için hazırlık 'Araç/Kargo Ataması' aşamasında olmalıdır.");

            if (!zp.IsFrozen)
                throw new DomainException("Hazırlık başlatılmadan kargo ataması yapılamaz.");

            if (!zp.IrsaliyeFetched)
                throw new DomainException("Kargo göndermeden önce Netsisten irsaliye numaraları çekilmelidir.");

            var useYurtici = request.CargoProvider == CargoProvider.YurticiKargo;

            var query = _context.WarehouseShipments
                .Where(s =>
                    s.ZonePreparationId == zp.Id &&
                    s.Status != ShipmentStatus.Cancelled &&
                    s.Status != ShipmentStatus.Passive &&
                    s.Status < ShipmentStatus.Dispatched);

            if (useYurtici)
                query = query.Include(s => s.Project).Include(s => s.IssOrder);

            var shipments = await query.ToListAsync(cancellationToken);

            foreach (var s in shipments)
            {
                s.ChangeStatus(ShipmentStatus.Dispatched, _currentUserService.UserId);
                s.SetCargoDispatch(request.CargoProvider, request.CargoTrackingNumber);

                if (useYurtici && _ykClient != null)
                {
                    var totalCount = request.YkDesiLines?.Sum(l => l.Count) ?? 1;
                    var totalDesi  = request.YkDesiLines?.Sum(l => l.Count * l.Desi) ?? 1m;
                    await CreateYkShipmentAsync(s, totalCount, totalDesi, cancellationToken);
                }
            }

            zp.Status = ZonePreparationStatus.Dispatched;

            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }

        private async Task CreateYkShipmentAsync(
            Akyildiz.Sevkiyat.Domain.Entities.Shipment shipment,
            int pieceCount,
            decimal totalDesi,
            CancellationToken ct)
        {
            // Idempotency: daha önce başarıyla gönderildiyse tekrar çağırma
            if (!string.IsNullOrEmpty(shipment.YkCargoKey) &&
                shipment.YkOperationStatus is "0" or "S" or "SUCCESS")
            {
                _log.LogInformation(
                    "[YK] Sevkiyat zaten Yurtici Kargo'ya gönderilmiş, atlanıyor. ShipmentId={ShipmentId} CargoKey={CargoKey}",
                    shipment.Id, shipment.YkCargoKey);
                return;
            }

            var cargoKey   = $"{CargoKeyPrefix}{shipment.Id}";
            var invoiceKey = shipment.IrsaliyeNo ?? shipment.TalepNo ?? cargoKey;

            var phone = shipment.IssOrder?.TeslimAlacakTelefonNumaralari
                ?.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p))
                ?? "05000000000";

            var receiverName = shipment.Project != null
                ? $"{shipment.Project.Code} - {shipment.Project.Name}"
                : "Bilinmiyor";

            var req = new YkCreateShipmentRequest(
                CargoKey:        cargoKey,
                InvoiceKey:      invoiceKey,
                ReceiverName:    receiverName,
                ReceiverAddress: shipment.Project?.Address ?? "Adres bilgisi yok",
                ReceiverPhone:   phone,
                PieceCount:      pieceCount,
                Desi:            totalDesi
            );

            var result = await _ykClient!.CreateShipmentAsync(req, ct);

            // Başarılı veya başarısız — tüm sonucu sakla (debug için)
            shipment.SetYkCargoInfo(
                cargoKey:         cargoKey,
                invoiceKey:       result.InvoiceKey ?? invoiceKey,
                jobId:            result.JobId,
                barcode:          result.Barcode,
                operationStatus:  result.OperationStatus,
                operationMessage: result.OperationMessage,
                errorCode:        result.ErrCode,
                errorMessage:     result.ErrMessage
            );

            if (!result.IsSuccess)
            {
                _log.LogWarning(
                    "[YK] Kargo oluşturulamadı — sevkiyat yine de gönderildi, YkCargoKey hata detayıyla kaydedildi. " +
                    "ShipmentId={ShipmentId} CargoKey={CargoKey} ErrorMessage={ErrorMessage} ErrCode={ErrCode} ErrMsg={ErrMsg}",
                    shipment.Id, cargoKey, result.ErrorMessage, result.ErrCode, result.ErrMessage);
            }
        }

        private const string CargoKeyPrefix = "AKY-";
    }
}
