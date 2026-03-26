using Akyildiz.Sevkiyat.Application.StockLocations.Commands.AssignStockToLocation;
using Akyildiz.Sevkiyat.Application.StockLocations.Commands.TransferStock;
using Akyildiz.Sevkiyat.Application.StockLocations.Queries.GetStockLocations;
using Akyildiz.Sevkiyat.Application.StockLocations.Queries.GetTransferHistory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/stock-locations")]
    public class StockLocationsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public StockLocationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// GET /api/stock-locations?stockMasterId=1  → bir stokun lokasyonları
        /// GET /api/stock-locations?warehouseLocationId=5 → bir lokasyonun stokları
        /// GET /api/stock-locations → tüm stok-lokasyon satırları
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Manager,Warehouse,Accounting")]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? stockMasterId = null,
            [FromQuery] int? warehouseLocationId = null,
            CancellationToken ct = default)
        {
            var result = await _mediator.Send(
                new GetStockLocationsQuery(stockMasterId, warehouseLocationId), ct);
            return Ok(result);
        }

        /// <summary>
        /// Stoku lokasyona ata (ilk yerleştirme / düzeltme).
        /// Miktar doğrudan set edilir (toplam stok değişmez).
        /// </summary>
        [HttpPost("assign")]
        [Authorize(Roles = "Admin,Manager,Warehouse")]
        public async Task<IActionResult> Assign(
            [FromBody] AssignStockToLocationRequest request,
            CancellationToken ct = default)
        {
            var id = await _mediator.Send(
                new AssignStockToLocationCommand(
                    request.StockMasterId,
                    request.WarehouseLocationId,
                    request.Qty), ct);
            return Ok(new { id });
        }

        /// <summary>
        /// Lokasyonlar arası stok transferi.
        /// </summary>
        [HttpPost("transfer")]
        [Authorize(Roles = "Admin,Manager,Warehouse")]
        public async Task<IActionResult> Transfer(
            [FromBody] TransferStockRequest request,
            CancellationToken ct = default)
        {
            var userId = GetCurrentUserId();
            var id = await _mediator.Send(
                new TransferStockCommand(
                    request.StockMasterId,
                    request.FromLocationId,
                    request.ToLocationId,
                    request.Qty,
                    request.Note,
                    userId), ct);
            return Ok(new { id });
        }

        /// <summary>
        /// Transfer geçmişi.
        /// </summary>
        [HttpGet("transfers")]
        [Authorize(Roles = "Admin,Manager,Warehouse,Accounting")]
        public async Task<IActionResult> GetTransferHistory(
            [FromQuery] int? stockMasterId = null,
            [FromQuery] int? locationId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken ct = default)
        {
            var result = await _mediator.Send(
                new GetTransferHistoryQuery(stockMasterId, locationId, page, pageSize), ct);
            return Ok(result);
        }

        private int? GetCurrentUserId()
        {
            var sub = User.FindFirst("sub")?.Value;
            return int.TryParse(sub, out var id) ? id : null;
        }
    }

    public record AssignStockToLocationRequest(
        int     StockMasterId,
        int     WarehouseLocationId,
        decimal Qty
    );

    public record TransferStockRequest(
        int     StockMasterId,
        int     FromLocationId,
        int     ToLocationId,
        decimal Qty,
        string? Note
    );
}
