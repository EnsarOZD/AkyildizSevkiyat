using Akyildiz.Sevkiyat.Application.Reports.Queries.GetZoneMaterialReport;
using Akyildiz.Sevkiyat.Application.Reports.Queries.GetShipmentSummary;
using Akyildiz.Sevkiyat.Application.Reports.Queries.GetOpenPurchaseOrders;
using Akyildiz.Sevkiyat.Application.Reports.Queries.GetPendingGoodsReceipts;
using Akyildiz.Sevkiyat.Application.Reports.Queries.GetShipmentPerformance;
using Akyildiz.Sevkiyat.Application.Reports.Queries.GetStockStatusReport;
using Akyildiz.Sevkiyat.Application.Reports.Queries.GetReturnsReport;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly ISender _mediator;

        public ReportsController(ISender mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("zone-material")]
        public async Task<ActionResult<List<ZoneMaterialRowDto>>> GetZoneMaterialReport(
            [FromQuery] DateTime deliveryDate,
            [FromQuery] int? zoneId,
            [FromQuery] bool includeDelivered = false,
            [FromQuery] string qtyMode = "Ordered")
        {
            if (deliveryDate == default)
            {
                deliveryDate = DateTime.Today;
            }

            if (!Enum.TryParse<QtyMode>(qtyMode, true, out var mode))
            {
                mode = QtyMode.Ordered;
            }

            var query = new GetZoneMaterialReportQuery(deliveryDate, zoneId, includeDelivered, mode);
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("shipment-summary")]
        public async Task<ActionResult<ShipmentSummaryDto>> GetShipmentSummary(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] int? zoneId)
        {
            if (startDate == default) startDate = DateTime.Today.AddDays(-30);
            if (endDate == default)   endDate   = DateTime.Today;
            var result = await _mediator.Send(new GetShipmentSummaryQuery(startDate, endDate, zoneId));
            return Ok(result);
        }

        [HttpGet("open-purchase-orders")]
        [Authorize(Roles = "Admin,Accounting,Manager")]
        public async Task<ActionResult<List<OpenPurchaseOrderRow>>> GetOpenPurchaseOrders()
        {
            var result = await _mediator.Send(new GetOpenPurchaseOrdersQuery());
            return Ok(result);
        }

        [HttpGet("pending-goods-receipts")]
        [Authorize(Roles = "Admin,Accounting,Warehouse,Manager")]
        public async Task<ActionResult<List<PendingGoodsReceiptRow>>> GetPendingGoodsReceipts()
        {
            var result = await _mediator.Send(new GetPendingGoodsReceiptsQuery());
            return Ok(result);
        }

        [HttpGet("shipment-performance")]
        public async Task<ActionResult<ShipmentPerformanceDto>> GetShipmentPerformance(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] int? zoneId)
        {
            if (startDate == default) startDate = DateTime.Today.AddDays(-30);
            if (endDate == default)   endDate   = DateTime.Today;
            var result = await _mediator.Send(new GetShipmentPerformanceQuery(startDate, endDate, zoneId));
            return Ok(result);
        }

        [HttpGet("stock-status")]
        [Authorize(Roles = "Admin,Accounting,Warehouse,Manager")]
        public async Task<ActionResult<StockStatusReportDto>> GetStockStatus(
            [FromQuery] bool criticalOnly = false)
        {
            var result = await _mediator.Send(new GetStockStatusReportQuery(criticalOnly));
            return Ok(result);
        }

        [HttpGet("returns")]
        public async Task<ActionResult<ReturnsReportDto>> GetReturns(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] int? zoneId)
        {
            if (startDate == default) startDate = DateTime.Today.AddDays(-30);
            if (endDate == default)   endDate   = DateTime.Today;
            var result = await _mediator.Send(new GetReturnsReportQuery(startDate, endDate, zoneId));
            return Ok(result);
        }
    }
}
