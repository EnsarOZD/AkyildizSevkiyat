using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Orders.Commands.CheckNetsisTransfers;
using Akyildiz.Sevkiyat.Application.Orders.Commands.CheckOrderMappingStatus;
using Akyildiz.Sevkiyat.Application.Orders.Commands.CheckSingleOrderNetsis;
using Akyildiz.Sevkiyat.Application.Orders.Commands.ImportIssOrders;
using Akyildiz.Sevkiyat.Application.Orders.Commands.StartImportBatch;
using Akyildiz.Sevkiyat.Application.Orders.Commands.BulkDeactivateIssOrders;
using Akyildiz.Sevkiyat.Application.Orders.Commands.ToggleActive;
using Akyildiz.Sevkiyat.Application.Orders.Queries.GetImportBatches;
using Akyildiz.Sevkiyat.Application.Orders.Queries.GetImportBatchStatus;
using Akyildiz.Sevkiyat.Application.Orders.Queries.GetImportedOrders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class IssOrdersController : ControllerBase
    {
        private readonly ISender _mediator;
        private readonly ILogger<IssOrdersController> _logger;

        public IssOrdersController(ISender mediator, ILogger<IssOrdersController> logger)
        {
            _mediator = mediator;
            _logger = logger;
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

        [HttpPost("import-async")]
        public async Task<IActionResult> ImportAsync(
            [FromBody] StartImportBatchCommand command,
            [FromServices] IServiceScopeFactory scopeFactory)
        {
            // Create the batch record immediately and return its ID
            var batchId = await _mediator.Send(command);

            // Run the actual import in background (no HTTP timeout concern)
            _ = Task.Run(async () =>
            {
                using var scope = scopeFactory.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<IIssOrderImportOrchestrator>();
                try
                {
                    await orchestrator.RunAsync(batchId, command.StartDate, command.EndDate, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background import failed for batch {BatchId}", batchId);
                }
            });

            return Ok(new { batchId });
        }

        [HttpGet("import-batches/{id:int}")]
        public async Task<IActionResult> GetImportBatchStatus(int id)
        {
            var result = await _mediator.Send(new GetImportBatchStatusQuery(id));
            if (result == null) return NotFound();
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

        [HttpPost("bulk-deactivate")]
        [Authorize(Roles = "Admin,Manager,Accounting")]
        public async Task<IActionResult> BulkDeactivate([FromBody] BulkDeactivateRequest request)
        {
            var count = await _mediator.Send(new BulkDeactivateIssOrdersCommand(request.Ids));
            return Ok(new { count });
        }

        public record BulkDeactivateRequest(IReadOnlyList<int> Ids);

        [HttpPost("check-mappings")]
        public async Task<IActionResult> CheckMappings()
        {
            var count = await _mediator.Send(new CheckOrderMappingStatusCommand());
            return Ok(new { Count = count });
        }

        [HttpPost("check-netsis-transfers")]
        [Authorize(Roles = "Admin,Accounting,Manager")]
        public async Task<IActionResult> CheckNetsisTransfers([FromBody] CheckNetsisTransfersRequest? body)
        {
            var command = new CheckNetsisTransfersCommand
            {
                FromDate         = body?.FromDate,
                ToDate           = body?.ToDate,
                CheckTransferred = body?.CheckTransferred ?? false,
            };
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("check-single-order-netsis")]
        [Authorize(Roles = "Admin,Accounting,Manager")]
        public async Task<IActionResult> CheckSingleOrderNetsis([FromBody] CheckSingleOrderRequest body)
        {
            var result = await _mediator.Send(new CheckSingleOrderNetsisCommand(body.OrderNumber));
            return Ok(result);
        }
    }

    public record CheckNetsisTransfersRequest(DateTime? FromDate, DateTime? ToDate, bool CheckTransferred = false);
    public record CheckSingleOrderRequest(string OrderNumber);
}
