using Akyildiz.Sevkiyat.Application.Netsis.Commands.BulkExportShipmentsToNetsis;
using Akyildiz.Sevkiyat.Application.Netsis.Commands.RecoverNetsisTransfers;
using Akyildiz.Sevkiyat.Application.Netsis.Commands.ExportClothingShipmentToNetsis;
using Akyildiz.Sevkiyat.Application.Netsis.Commands.ExportPurchaseOrderToNetsis;
using Akyildiz.Sevkiyat.Application.Netsis.Commands.ExportShipmentToNetsis;
using Akyildiz.Sevkiyat.Application.Netsis.Commands.FetchShipmentIrsaliye;
using Akyildiz.Sevkiyat.Application.Netsis.Commands.SyncNetsisStockBalance;
using Akyildiz.Sevkiyat.Application.Netsis.Commands.VerifyNetsisShipmentTransfers;
using Akyildiz.Sevkiyat.Application.Netsis.Queries.GetReconciliation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [ApiController]
    [Route("api/netsis")]
    [Authorize]
    public class NetsisController : ControllerBase
    {
        private readonly IMediator _mediator;

        public NetsisController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Belirtilen sevkiyatı Netsis'e "Müşteri Siparişi" olarak aktarır.
        /// Önkoşul: Sevkiyat ReadyForDispatch veya AssignedToVehicle durumunda olmalı.
        /// </summary>
        [HttpPost("shipments/{id:int}/export")]
        [Authorize(Roles = "Admin,Manager,Accounting")]
        public async Task<IActionResult> ExportShipment(int id, CancellationToken ct)
        {
            var result = await _mediator.Send(new ExportShipmentToNetsisCommand(id), ct);
            return Ok(new
            {
                netsisOrderNo = result.NetsisOrderNo,
                warnings = result.Warnings,
                message = $"Sevkiyat #{id} Netsis'e aktarıldı. Belge No: {result.NetsisOrderNo}"
            });
        }

        /// <summary>
        /// Kıyafet operasyonu: Sevkiyatı Netsis'e aktarır ve doğrudan Delivered durumuna taşır.
        /// Önkoşul: Clothing tipi, Created durumu, NetsisTransferredAt == null.
        /// </summary>
        [HttpPost("shipments/{id:int}/export-clothing")]
        [Authorize(Roles = "Admin,Manager,Accounting")]
        public async Task<IActionResult> ExportClothingShipment(int id, CancellationToken ct)
        {
            var result = await _mediator.Send(new ExportClothingShipmentToNetsisCommand(id), ct);
            return Ok(new
            {
                netsisOrderNo = result.NetsisOrderNo,
                irsaliyeNo    = result.IrsaliyeNo,
                warnings      = result.Warnings,
                message       = $"Kıyafet sevkiyatı #{id} Netsis'e aktarıldı ve teslim edildi olarak işaretlendi."
            });
        }

        /// <summary>
        /// Birden fazla sevkiyatı toplu olarak Netsis'e "Müşteri Siparişi" olarak aktarır.
        /// </summary>
        [HttpPost("shipments/bulk-export")]
        [Authorize(Roles = "Admin,Manager,Accounting")]
        public async Task<IActionResult> BulkExportShipments([FromBody] BulkExportBody body, CancellationToken ct)
        {
            if (body.ShipmentIds == null || body.ShipmentIds.Count == 0)
                return BadRequest("En az bir sevkiyat ID'si gereklidir.");
            var result = await _mediator.Send(new BulkExportShipmentsToNetsisCommand(body.ShipmentIds), ct);
            return Ok(result);
        }

        public record BulkExportBody(List<int> ShipmentIds);

        /// <summary>
        /// Netsis'e aktarılmış görünen sevkiyatların siparişlerini Netsis'te doğrular.
        /// Netsis'te artık mevcut olmayan (silinmiş) siparişlerin NetsisTransferredAt'ını sıfırlar.
        /// Ek olarak, henüz aktarılmamış ama Netsis'te mevcut olan siparişleri otomatik işaretler.
        /// </summary>
        [HttpPost("shipments/verify-transfers")]
        [Authorize(Roles = "Admin,Manager,Accounting")]
        public IActionResult VerifyTransfers(
            [FromBody] VerifyNetsisShipmentTransfersCommand command,
            [FromServices] IServiceScopeFactory scopeFactory)
        {
            _ = Task.Run(async () =>
            {
                using var scope = scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var logger   = scope.ServiceProvider.GetRequiredService<ILogger<NetsisController>>();
                try
                {
                    await mediator.Send(command, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Background VerifyNetsisShipmentTransfers failed");
                }
            });

            return Accepted(new { message = "Netsis durum kontrolü arka planda başlatıldı. Birkaç dakika içinde tamamlanacak." });
        }

        /// <summary>
        /// Hatalı Netsis durum kontrolü nedeniyle irsaliyesi silinmiş ve durumu geri alınmış
        /// sevkiyatları kurtarır. AssignedToVehicle / Dispatched / ReturnedToWarehouse durumundaki,
        /// NetsisTransferredAt'ı boş olan sevkiyatlar Netsis'te aranır; bulunanlar ReadyForDispatch'e alınır.
        /// </summary>
        [HttpPost("shipments/recover-transfers")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RecoverTransfers(CancellationToken ct)
        {
            var result = await _mediator.Send(new RecoverNetsisTransfersCommand(), ct);
            return Ok(result);
        }

        /// <summary>
        /// Netsis ile toplanan/teslim edilen miktar uzlaştırma raporu.
        /// Sadece NetsisTransferredAt dolu sevkiyatlar dahil edilir.
        /// </summary>
        [HttpGet("reconciliation")]
        [Authorize(Roles = "Admin,Manager,Accounting")]
        public async Task<IActionResult> GetReconciliation(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] bool onlyDiff = false,
            [FromQuery] int? operationType = null,
            [FromQuery] bool? mailSent = null,
            CancellationToken ct = default)
        {
            var result = await _mediator.Send(
                new GetReconciliationQuery(fromDate, toDate, onlyDiff, operationType, mailSent), ct);
            return Ok(result);
        }

        /// <summary>
        /// Belirtilen sevkiyat için Netsis'ten irsaliye numarasını çeker.
        /// Önkoşul: NetsisTransferredAt dolu olmalı.
        /// </summary>
        [HttpPost("shipments/{id:int}/fetch-irsaliye")]
        [Authorize(Roles = "Admin,Manager,Accounting,Warehouse")]
        public async Task<IActionResult> FetchShipmentIrsaliye(int id, CancellationToken ct)
        {
            var result = await _mediator.Send(new FetchShipmentIrsaliyeCommand(id), ct);
            return Ok(new { irsaliyeNo = result.IrsaliyeNo, message = result.Message });
        }

        /// <summary>
        /// Onaylı satınalma siparişini Netsis'e "Satınalma Siparişi" (FaturaTip=6) olarak aktarır.
        /// Önkoşul: Status == Approved, NetsisTransferredAt == null, Supplier.SupplierCode dolu.
        /// </summary>
        [HttpPost("purchase-orders/{id:guid}/export")]
        [Authorize(Roles = "Admin,Manager,Accounting")]
        public async Task<IActionResult> ExportPurchaseOrder(Guid id, CancellationToken ct)
        {
            var netsisPONo = await _mediator.Send(new ExportPurchaseOrderToNetsisCommand(id), ct);
            return Ok(new
            {
                netsisPONo,
                message = $"Satınalma siparişi Netsis'e aktarıldı. Belge No: {netsisPONo}"
            });
        }

        /// <summary>
        /// Netsis'ten anlık stok bakiyelerini çeker ve StockMaster.OnHandQty'yi günceller.
        /// stokKodu parametresi verilmezse tüm stoklar senkronize edilir.
        /// </summary>
        [HttpPost("stock/sync")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> SyncStockBalance([FromQuery] string? stokKodu, CancellationToken ct)
        {
            var result = await _mediator.Send(new SyncNetsisStockBalanceCommand(stokKodu), ct);
            return Ok(result);
        }
    }
}
