using Akyildiz.Sevkiyat.Application.Stocks.Commands.AutoMatchStockMappings;
using Akyildiz.Sevkiyat.Application.Stocks.Commands.ImportStockMappings;
using Akyildiz.Sevkiyat.Application.Stocks.Commands.MapStock;
using Akyildiz.Sevkiyat.Application.Stocks.Queries.GetAllStockMappings;
using Akyildiz.Sevkiyat.Application.Stocks.Queries.GetUnmappedStocks;
using Akyildiz.Sevkiyat.Application.Stocks.Queries.ExportUnmappedStocks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class StockMappingsController : ControllerBase
    {
        private readonly ISender _mediator;

        public StockMappingsController(ISender mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? statusFilter,
            [FromQuery] string? search,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            var result = await _mediator.Send(new GetAllStockMappingsQuery
            {
                StatusFilter = statusFilter,
                Search       = search,
                PageNumber   = pageNumber,
                PageSize     = pageSize,
            });
            return Ok(result);
        }

        [HttpGet("unmapped")]
        public async Task<IActionResult> GetUnmapped()
        {
            var result = await _mediator.Send(new GetUnmappedStocksQuery());
            return Ok(result);
        }

        [HttpGet("export-unmapped")]
        public async Task<IActionResult> ExportUnmapped()
        {
            var result = await _mediator.Send(new ExportUnmappedStocksQuery());
            return File(result.Content, result.ContentType, result.FileName);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> MapStock(int id, [FromBody] MapStockCommand command)
        {
            if (id != command.MappingId) return BadRequest();
            await _mediator.Send(command);
            return NoContent();
        }
        [HttpPost("import")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("File is empty");

            using var stream = file.OpenReadStream();
            var result = await _mediator.Send(new ImportStockMappingsCommand(stream));
            return Ok(result);
        }

        [HttpPost("auto-match")]
        public async Task<IActionResult> AutoMatch()
        {
            var result = await _mediator.Send(new AutoMatchStockMappingsCommand());
            return Ok(result);
        }
    }
}
