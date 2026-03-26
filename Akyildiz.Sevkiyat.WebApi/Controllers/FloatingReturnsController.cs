using Akyildiz.Sevkiyat.Application.FloatingReturns.Commands.CreateFloatingReturn;
using Akyildiz.Sevkiyat.Application.FloatingReturns.Commands.ResolveFloatingReturn;
using Akyildiz.Sevkiyat.Application.FloatingReturns.Queries.GetFloatingReturns;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FloatingReturnsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FloatingReturnsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] FloatingReturnStatus? status,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            var result = await _mediator.Send(new GetFloatingReturnsQuery(status, fromDate, toDate));
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager,Warehouse,Dispatcher")]
        public async Task<IActionResult> Create([FromBody] CreateFloatingReturnRequest request)
        {
            var id = await _mediator.Send(new CreateFloatingReturnCommand(
                request.ReturnDate,
                request.StockMasterId,
                request.StockCodeFree,
                request.StockNameFree,
                request.Qty,
                request.ReturnReason,
                request.Note
            ));
            return CreatedAtAction(nameof(GetAll), new { id }, new { id });
        }

        [HttpPost("{id:int}/resolve")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Resolve(int id, [FromBody] ResolveFloatingReturnRequest request)
        {
            await _mediator.Send(new ResolveFloatingReturnCommand(
                id,
                request.Action,
                request.LinkedShipmentId,
                request.Note
            ));
            return NoContent();
        }
    }

    public record CreateFloatingReturnRequest(
        DateTime ReturnDate,
        int? StockMasterId,
        string? StockCodeFree,
        string? StockNameFree,
        decimal Qty,
        ReturnReason ReturnReason,
        string? Note
    );

    public record ResolveFloatingReturnRequest(
        ResolveAction Action,
        int? LinkedShipmentId,
        string? Note
    );
}
