using Akyildiz.Sevkiyat.Application.VehicleReturns.Commands.CreateVehicleReturn;
using Akyildiz.Sevkiyat.Application.VehicleReturns.Commands.ResolveVehicleReturnLine;
using Akyildiz.Sevkiyat.Application.VehicleReturns.Queries.GetVehicleReturns;
using Akyildiz.Sevkiyat.Application.VehicleReturns.Queries.SearchVehicleShipments;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class VehicleReturnsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public VehicleReturnsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager,Accounting")]
        public async Task<IActionResult> GetAll(
            [FromQuery] Guid? sessionId,
            [FromQuery] VehicleReturnLineStatus? lineStatus,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _mediator.Send(new GetVehicleReturnsQuery(sessionId, lineStatus, fromDate, toDate, pageNumber, pageSize));
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager,Accounting,Warehouse")]
        public async Task<IActionResult> Create([FromBody] CreateVehicleReturnRequest request)
        {
            var lines = request.Lines.Select(l => new CreateVehicleReturnLineInput(
                l.StockMasterId, l.StockCodeFree, l.StockNameFree, l.Qty, l.Note
            )).ToList();

            var id = await _mediator.Send(new CreateVehicleReturnCommand(
                request.DriverSessionId, request.ReturnDate, request.Note, lines
            ));

            return CreatedAtAction(nameof(GetAll), new { id }, new { id });
        }

        [HttpPost("lines/{lineId:int}/resolve")]
        [Authorize(Roles = "Admin,Manager,Accounting")]
        public async Task<IActionResult> ResolveLine(int lineId, [FromBody] ResolveVehicleReturnLineRequest request)
        {
            await _mediator.Send(new ResolveVehicleReturnLineCommand(
                lineId, request.Action, request.LinkedShipmentId, request.Note
            ));
            return NoContent();
        }

        [HttpGet("search-shipments")]
        [Authorize(Roles = "Admin,Manager,Accounting,Warehouse")]
        public async Task<IActionResult> SearchShipments([FromQuery] Guid sessionId, [FromQuery] string search)
        {
            var result = await _mediator.Send(new SearchVehicleShipmentsQuery(sessionId, search));
            return Ok(result);
        }
    }

    public record CreateVehicleReturnLineRequest(
        int? StockMasterId,
        string? StockCodeFree,
        string? StockNameFree,
        decimal Qty,
        string? Note
    );

    public record CreateVehicleReturnRequest(
        Guid DriverSessionId,
        DateTime? ReturnDate,
        string? Note,
        List<CreateVehicleReturnLineRequest> Lines
    );

    public record ResolveVehicleReturnLineRequest(
        VehicleReturnResolveAction Action,
        int? LinkedShipmentId,
        string? Note
    );
}
