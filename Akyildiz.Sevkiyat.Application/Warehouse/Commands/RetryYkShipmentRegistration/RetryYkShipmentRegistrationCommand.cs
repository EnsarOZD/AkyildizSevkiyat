using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.External.YurtiKargo;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.RetryYkShipmentRegistration
{
    /// <summary>
    /// Daha önce YK'ya kayıt edilemeyen (createShipment çağrılmamış/başarısız) sevkiyatları
    /// tekrar kayıt etmeye çalışır. Admin/Warehouse rolüne açıktır.
    /// </summary>
    public record RetryYkShipmentRegistrationCommand : IRequest<RetryYkResult>, IRequireRoles
    {
        public int ShipmentId { get; init; }
        public int PieceCount { get; init; } = 1;
        public decimal Desi { get; init; } = 1m;

        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Warehouse" };
    }

    public class RetryYkResult
    {
        public bool IsSuccess { get; set; }
        public string? Barcode { get; set; }
        public string? OperationStatus { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class RetryYkShipmentRegistrationCommandHandler
        : IRequestHandler<RetryYkShipmentRegistrationCommand, RetryYkResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly IYurtiKargoClient _ykClient;
        private readonly ILogger<RetryYkShipmentRegistrationCommandHandler> _log;

        private const string CargoKeyPrefix = "AKY-";

        public RetryYkShipmentRegistrationCommandHandler(
            IApplicationDbContext context,
            IYurtiKargoClient ykClient,
            ILogger<RetryYkShipmentRegistrationCommandHandler> log)
        {
            _context  = context;
            _ykClient = ykClient;
            _log      = log;
        }

        public async Task<RetryYkResult> Handle(
            RetryYkShipmentRegistrationCommand request,
            CancellationToken cancellationToken)
        {
            var shipment = await _context.Shipments
                .Include(s => s.Project)
                .Include(s => s.IssOrder)
                .FirstOrDefaultAsync(s => s.Id == request.ShipmentId, cancellationToken)
                ?? throw new NotFoundException("Shipment", request.ShipmentId);

            if (shipment.CargoProvider != CargoProvider.YurticiKargo)
                throw new DomainException("Bu sevkiyat Yurtici Kargo ile gönderilmemiş.");

            if (shipment.Status < Domain.Enums.ShipmentStatus.Dispatched)
                throw new DomainException("Sevkiyat henüz gönderilemedi statüsünde değil.");

            // Zaten başarılıysa tekrar gönderme
            if (shipment.YkOperationStatus is "0" && !string.IsNullOrWhiteSpace(shipment.YkBarcode))
                return new RetryYkResult
                {
                    IsSuccess       = true,
                    Barcode         = shipment.YkBarcode,
                    OperationStatus = shipment.YkOperationStatus,
                    ErrorMessage    = "Sevkiyat zaten başarıyla kayıtlı."
                };

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
                PieceCount:      request.PieceCount,
                Desi:            request.Desi
            );

            _log.LogInformation(
                "[YK] createShipment yeniden deneniyor. ShipmentId={ShipmentId} CargoKey={CargoKey}",
                shipment.Id, cargoKey);

            var result = await _ykClient.CreateShipmentAsync(req, cancellationToken);

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

            await _context.SaveChangesAsync(cancellationToken);

            if (result.IsSuccess)
            {
                _log.LogInformation(
                    "[YK] createShipment başarılı (retry). ShipmentId={ShipmentId} CargoKey={CargoKey} Barcode={Barcode}",
                    shipment.Id, cargoKey, result.Barcode ?? "(boş)");
            }
            else
            {
                _log.LogWarning(
                    "[YK] createShipment başarısız (retry). ShipmentId={ShipmentId} CargoKey={CargoKey} ErrCode={ErrCode} ErrMsg={ErrMsg}",
                    shipment.Id, cargoKey, result.ErrCode, result.ErrMessage);
            }

            return new RetryYkResult
            {
                IsSuccess       = result.IsSuccess,
                Barcode         = result.Barcode,
                OperationStatus = result.OperationStatus,
                ErrorMessage    = result.IsSuccess ? null : result.ErrorMessage,
            };
        }
    }
}
