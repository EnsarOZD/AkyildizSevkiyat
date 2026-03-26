using Akyildiz.Sevkiyat.Application.PurchaseOrders.Commands.AddPurchaseOrderLine;
using Akyildiz.Sevkiyat.Application.PurchaseOrders.Commands.ApprovePurchaseOrder;
using Akyildiz.Sevkiyat.Application.PurchaseOrders.Commands.CancelPurchaseOrder;
using Akyildiz.Sevkiyat.Application.PurchaseOrders.Commands.ClosePurchaseOrder;
using Akyildiz.Sevkiyat.Application.PurchaseOrders.Commands.CreatePurchaseOrder;
using Akyildiz.Sevkiyat.Application.PurchaseOrders.Commands.UpdatePurchaseOrder;
using Akyildiz.Sevkiyat.Application.PurchaseOrders.Commands.UpdatePurchaseOrderLine;
using Akyildiz.Sevkiyat.Application.PurchaseOrders.Commands.RemovePurchaseOrderLine;
using Akyildiz.Sevkiyat.Application.PurchaseOrders.Queries.GetPurchaseOrderDetail;
using Akyildiz.Sevkiyat.Application.PurchaseOrders.Queries.GetPurchaseOrders;
using Akyildiz.Sevkiyat.Application.PurchaseOrders.Queries.GetReceivablePurchaseOrders;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Authorization;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [Authorize(Roles = "Admin,Accounting,Manager")]
    [ApiController]
    [Route("api/purchase-orders")]
    public class PurchaseOrdersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PurchaseOrdersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] GetPurchaseOrdersQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("receivable")]
        public async Task<IActionResult> GetReceivable([FromQuery] GetReceivablePurchaseOrdersQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetail(Guid id)
        {
            var result = await _mediator.Send(new GetPurchaseOrderDetailQuery { Id = id });
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreatePurchaseOrderCommand command)
        {
            var id = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetDetail), new { id }, id);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, UpdatePurchaseOrderCommand command)
        {
            if (id != command.Id) return BadRequest("ID mismatch");

            await _mediator.Send(command);
            return Ok();
        }

        [HttpPost("{id}/lines")]
        public async Task<IActionResult> AddLine(Guid id, AddPurchaseOrderLineCommand command)
        {
            if (id != command.PurchaseOrderId) return BadRequest("ID mismatch");

            var lineId = await _mediator.Send(command);
            return Ok(lineId);
        }

        [HttpPut("{id}/lines/{lineId}")]
        public async Task<IActionResult> UpdateLine(Guid id, Guid lineId, UpdatePurchaseOrderLineCommand command)
        {
            if (id != command.PurchaseOrderId || lineId != command.LineId) return BadRequest("ID mismatch");

            await _mediator.Send(command);
            return Ok();
        }

        [HttpDelete("{id}/lines/{lineId}")]
        public async Task<IActionResult> RemoveLine(Guid id, Guid lineId)
        {
            await _mediator.Send(new RemovePurchaseOrderLineCommand { PurchaseOrderId = id, LineId = lineId });
            return Ok();
        }

        [HttpPost("{id}/approve")]
        public async Task<IActionResult> Approve(Guid id)
        {
            await _mediator.Send(new ApprovePurchaseOrderCommand { Id = id });
            return Ok();
        }

        [HttpPost("{id}/close")]
        public async Task<IActionResult> Close(Guid id)
        {
            await _mediator.Send(new ClosePurchaseOrderCommand { Id = id });
            return Ok();
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(Guid id)
        {
            await _mediator.Send(new CancelPurchaseOrderCommand { Id = id });
            return Ok();
        }
    }
}
