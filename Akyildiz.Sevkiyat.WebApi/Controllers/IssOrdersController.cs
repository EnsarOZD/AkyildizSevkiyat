using Akyildiz.Sevkiyat.Application.Orders.Commands.CheckOrderMappingStatus;
using Akyildiz.Sevkiyat.Application.Orders.Commands.ImportIssOrders;
using Akyildiz.Sevkiyat.Application.Orders.Commands.ToggleActive;
using Akyildiz.Sevkiyat.Application.Orders.Queries.GetImportBatches;
using Akyildiz.Sevkiyat.Application.Orders.Queries.GetImportedOrders;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IssOrdersController : ControllerBase
    {
        private readonly ISender _mediator;

        public IssOrdersController(ISender mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] string tab = "Ready",
            [FromQuery] string? search = null,
            [FromQuery] string? zone = null,
            [FromQuery] string? talepNoStatus = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = new GetImportedOrdersQuery 
            { 
                Tab = tab,
                Search = search,
                Zone = zone,
                TalepNoStatus = talepNoStatus,
                PageNumber = page,
                PageSize = pageSize
            };
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("counts")]
        public async Task<IActionResult> GetCounts()
        {
            var result = await _mediator.Send(new GetIssOrderCountsQuery());
            return Ok(result);
        }

        [HttpPost("import")]
        public async Task<IActionResult> Import([FromBody] ImportIssOrdersCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpGet("import-batches")]
        public async Task<IActionResult> GetImportBatches([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _mediator.Send(new GetImportBatchesQuery(page, pageSize));
            return Ok(result);
        }

        [HttpPost("{id:int}/toggle-active")]
        public async Task<IActionResult> ToggleActive(int id, [FromQuery] bool isActive)
        {
            await _mediator.Send(new ToggleIssOrderActiveCommand(id, isActive));
            return NoContent();
        }

        [HttpPost("check-mappings")]
        public async Task<IActionResult> CheckMappings()
        {
            var count = await _mediator.Send(new CheckOrderMappingStatusCommand());
            return Ok(new { Count = count });
        }
    }
}
