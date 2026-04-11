using Akyildiz.Sevkiyat.Application.Common.Models;
using Akyildiz.Sevkiyat.Application.Stocks.Commands.CreateStock;
using Akyildiz.Sevkiyat.Application.Stocks.Commands.DeleteStock;
using Akyildiz.Sevkiyat.Application.Stocks.Commands.ImportStocks;
using Akyildiz.Sevkiyat.Application.Stocks.Commands.UpdateStock;
using Akyildiz.Sevkiyat.Application.Stocks.Commands.UpdateStockNetsisCode;
using Akyildiz.Sevkiyat.Application.Stocks.Commands.UpdateStockThresholds;
using Akyildiz.Sevkiyat.Application.Stocks.Queries.GetStocks;
using Akyildiz.Sevkiyat.Application.Stocks.Queries.GetStocksTemplate;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    public record UpdateStockNetsisCodeBody(string? NetsisStockCode);

    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class StocksController : ControllerBase
    {
        private readonly ISender _mediator;

        public StocksController(ISender mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult<PaginatedList<StockDto>>> GetStocks([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int size = 15, [FromQuery] int? categoryId = null, [FromQuery] int? pickingTypeId = null, [FromQuery] int? unitId = null, [FromQuery] bool? isActive = null)
        {
            return await _mediator.Send(new GetStocksQuery(search, page, size, categoryId, pickingTypeId, unitId, isActive));
        }

        [HttpPost("import")]
        public async Task<ActionResult> ImportStocks(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            using var stream = file.OpenReadStream();
            var command = new ImportStocksCommand(stream);
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpGet("template")]
        public async Task<IActionResult> Template()
        {
            var result = await _mediator.Send(new GetStocksTemplateQuery());
            return File(result.Content, result.ContentType, result.FileName);
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export()
        {
            var result = await _mediator.Send(new Akyildiz.Sevkiyat.Application.Stocks.Queries.ExportStocks.ExportStocksQuery());
            return File(result.Content, result.ContentType, result.FileName);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Accounting,Manager")]
        public async Task<ActionResult<int>> Create(CreateStockCommand command)
        {
            return await _mediator.Send(command);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Accounting,Manager")]
        public async Task<ActionResult> Update(int id, UpdateStockCommand command)
        {
            if (id != command.Id) return BadRequest();
            await _mediator.Send(command);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Accounting,Manager")]
        public async Task<ActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteStockCommand(id));
            return NoContent();
        }

        [HttpPatch("{id}/netsis-code")]
        [Authorize(Roles = "Admin,Accounting,Manager")]
        public async Task<ActionResult> UpdateNetsisCode(int id, [FromBody] UpdateStockNetsisCodeBody body)
        {
            await _mediator.Send(new UpdateStockNetsisCodeCommand(id, body.NetsisStockCode));
            return NoContent();
        }

        [HttpPut("{id}/thresholds")]
        [Authorize(Roles = "Admin,Manager,Warehouse")]
        public async Task<ActionResult> UpdateThresholds(int id, UpdateStockThresholdsCommand command)
        {
            if (id != command.StockMasterId) return BadRequest();
            await _mediator.Send(command);
            return NoContent();
        }
    }
}
