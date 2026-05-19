using Akyildiz.Sevkiyat.Application.GoodsReceipts.Commands.AddGoodsReceiptLine;
using Akyildiz.Sevkiyat.Application.GoodsReceipts.Commands.CancelGoodsReceipt;
using Akyildiz.Sevkiyat.Application.GoodsReceipts.Commands.CreateGoodsReceipt;
using Akyildiz.Sevkiyat.Application.GoodsReceipts.Commands.PostGoodsReceipt;
using Akyildiz.Sevkiyat.Application.GoodsReceipts.Commands.RemoveGoodsReceiptLine;
using Akyildiz.Sevkiyat.Application.GoodsReceipts.Commands.CreateCorrectionGoodsReceipt;
using Akyildiz.Sevkiyat.Application.GoodsReceipts.Queries.GetGoodsReceiptDetail;
using Akyildiz.Sevkiyat.Application.GoodsReceipts.Queries.GetGoodsReceipts;
using Akyildiz.Sevkiyat.Application.GoodsReceipts.Commands.UpdateGoodsReceiptLine;
using Akyildiz.Sevkiyat.Application.GoodsReceipts.Commands.BatchUpdateLines;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Authorization;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/goods-receipts")]
    public class GoodsReceiptsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public GoodsReceiptsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Roles = "Admin,Warehouse,Manager,Accounting")]
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] GetGoodsReceiptsQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [Authorize(Roles = "Admin,Warehouse,Manager,Accounting")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetail(Guid id)
        {
            var result = await _mediator.Send(new GetGoodsReceiptDetailQuery { Id = id });
            return Ok(result);
        }

        [Authorize(Roles = "Admin,Warehouse,Manager,Accounting")]
        [HttpPost]
        public async Task<IActionResult> Create(CreateGoodsReceiptCommand command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [Authorize(Roles = "Admin,Warehouse,Manager,Accounting")]
        [HttpPost("{id}/lines")]
        public async Task<IActionResult> AddLine(Guid id, AddGoodsReceiptLineCommand command)
        {
            if (id != command.GoodsReceiptId) return BadRequest("ID mismatch");
            
            var lineId = await _mediator.Send(command);
            return Ok(lineId);
        }

        [Authorize(Roles = "Admin,Warehouse,Manager,Accounting")]
        [HttpPut("{id}/lines/{lineId}")]
        public async Task<IActionResult> UpdateLine(Guid id, Guid lineId, UpdateGoodsReceiptLineCommand command)
        {
            if (id != command.GoodsReceiptId || lineId != command.LineId) return BadRequest("ID mismatch");

            await _mediator.Send(command);
            return Ok();
        }

        [Authorize(Roles = "Admin,Warehouse,Manager,Accounting")]
        [HttpPut("{id}/lines")]
        public async Task<IActionResult> BatchUpdateLines(Guid id, BatchUpdateGoodsReceiptLinesCommand command)
        {
            if (id != command.GoodsReceiptId) return BadRequest("ID mismatch");

            await _mediator.Send(command);
            return Ok();
        }

        [Authorize(Roles = "Admin,Warehouse,Manager,Accounting")]
        [HttpPost("{id}/post")]
        public async Task<IActionResult> Post(Guid id)
        {
            await _mediator.Send(new PostGoodsReceiptCommand { Id = id });
            return Ok();
        }

        [Authorize(Roles = "Admin,Warehouse,Manager,Accounting")]
        [HttpDelete("{id}/lines/{lineId}")]
        public async Task<IActionResult> RemoveLine(Guid id, Guid lineId)
        {
            await _mediator.Send(new RemoveGoodsReceiptLineCommand { GoodsReceiptId = id, LineId = lineId });
            return Ok();
        }

        [Authorize(Roles = "Admin,Manager,Accounting")]
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(Guid id)
        {
            await _mediator.Send(new CancelGoodsReceiptCommand { Id = id });
            return Ok();
        }

        [Authorize(Roles = "Admin,Manager,Accounting")]
        [HttpPost("{id}/corrections")]
        public async Task<IActionResult> CreateCorrection(Guid id, CreateCorrectionGoodsReceiptCommand command)
        {
            if (id != command.OriginalGoodsReceiptId) return BadRequest("ID mismatch");

            var correctionId = await _mediator.Send(command);
            return Ok(correctionId);
        }
    }
}
