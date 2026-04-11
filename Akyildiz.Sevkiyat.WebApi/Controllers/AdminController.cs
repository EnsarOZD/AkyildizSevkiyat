using Akyildiz.Sevkiyat.Application.Admin.Commands.ForceCloseDriverSession;
using Akyildiz.Sevkiyat.Application.Admin.Queries.GetDriverSessions;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class AdminController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AdminController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("driver-sessions/{id:guid}/force-close")]
        public async Task<IActionResult> ForceClose(
            Guid id,
            [FromBody] ForceCloseRequest request,
            CancellationToken ct)
        {
            await _mediator.Send(new ForceCloseDriverSessionCommand(id, request.Notes), ct);
            return NoContent();
        }

        [HttpGet("driver-sessions")]
        [Authorize(Roles = "Admin,Manager,Accounting")]
        public async Task<ActionResult<GetDriverSessionsResult>> GetSessions(
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate,
            [FromQuery] int? driverId,
            [FromQuery] int? vehicleId,
            [FromQuery] DriverSessionStatus? status,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken ct = default)
        {
            return Ok(await _mediator.Send(
                new GetDriverSessionsQuery(fromDate, toDate, driverId, vehicleId, status, pageNumber, pageSize), ct));
        }
    }

    public record ForceCloseRequest(string? Notes);
}
