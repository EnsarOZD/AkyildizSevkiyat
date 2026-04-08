using Akyildiz.Sevkiyat.Application.Netsis.Commands.BulkExportShipmentsToNetsis;
using Akyildiz.Sevkiyat.Application.Netsis.Commands.ExportShipmentToNetsis;
using Akyildiz.Sevkiyat.Application.Netsis.Commands.SyncNetsisStockBalance;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [ApiController]
    [Route("api/netsis")]
    [Authorize(Roles = "Admin,Manager")]
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
        /// Birden fazla sevkiyatı toplu olarak Netsis'e "Müşteri Siparişi" olarak aktarır.
        /// </summary>
        [HttpPost("shipments/bulk-export")]
        public async Task<IActionResult> BulkExportShipments([FromBody] BulkExportBody body, CancellationToken ct)
        {
            if (body.ShipmentIds == null || body.ShipmentIds.Count == 0)
                return BadRequest("En az bir sevkiyat ID'si gereklidir.");
            var result = await _mediator.Send(new BulkExportShipmentsToNetsisCommand(body.ShipmentIds), ct);
            return Ok(result);
        }

        public record BulkExportBody(List<int> ShipmentIds);

        /// <summary>
        /// Netsis'ten anlık stok bakiyelerini çeker ve StockMaster.OnHandQty'yi günceller.
        /// stokKodu parametresi verilmezse tüm stoklar senkronize edilir.
        /// </summary>
        [HttpPost("stock/sync")]
        public async Task<IActionResult> SyncStockBalance([FromQuery] string? stokKodu, CancellationToken ct)
        {
            var result = await _mediator.Send(new SyncNetsisStockBalanceCommand(stokKodu), ct);
            return Ok(result);
        }
    }
}
