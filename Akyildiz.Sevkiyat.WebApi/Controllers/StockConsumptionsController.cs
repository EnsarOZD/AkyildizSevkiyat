using Akyildiz.Sevkiyat.Application.StockConsumptions.Commands.CreateStockConsumption;
using Akyildiz.Sevkiyat.Application.StockConsumptions.Commands.DeleteStockConsumption;
using Akyildiz.Sevkiyat.Application.StockConsumptions.Queries.ExportStockConsumptions;
using Akyildiz.Sevkiyat.Application.StockConsumptions.Queries.GetStockConsumptions;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/stock-consumptions")]
    public class StockConsumptionsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public StockConsumptionsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager,Warehouse,Accounting")]
        public async Task<IActionResult> GetAll(
            [FromQuery] DateOnly? fromDate,
            [FromQuery] DateOnly? toDate,
            [FromQuery] int? type,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int size = 30)
        {
            var result = await _mediator.Send(new GetStockConsumptionsQuery
            {
                FromDate = fromDate,
                ToDate = toDate,
                Type = type.HasValue ? (StockConsumptionType)type.Value : null,
                Search = search,
                PageNumber = page,
                PageSize = size
            });
            return Ok(result);
        }

        [HttpGet("export")]
        [Authorize(Roles = "Admin,Manager,Warehouse,Accounting")]
        public async Task<IActionResult> Export(
            [FromQuery] DateOnly? fromDate,
            [FromQuery] DateOnly? toDate,
            [FromQuery] int? type,
            [FromQuery] string? search)
        {
            var result = await _mediator.Send(new ExportStockConsumptionsQuery
            {
                FromDate = fromDate,
                ToDate = toDate,
                Type = type.HasValue ? (StockConsumptionType)type.Value : null,
                Search = search
            });
            return File(result.Content, result.ContentType, result.FileName);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager,Warehouse")]
        public async Task<IActionResult> Create([FromBody] CreateStockConsumptionCommand command)
        {
            var id = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetAll), new { }, new { id });
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteStockConsumptionCommand(id));
            return NoContent();
        }
    }
}
