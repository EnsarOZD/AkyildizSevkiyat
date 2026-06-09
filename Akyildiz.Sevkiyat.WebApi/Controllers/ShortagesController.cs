using Akyildiz.Sevkiyat.Application.ClothingPicking.Shortages;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [Authorize(Roles = "Admin,Manager,Accounting,Warehouse")]
    [ApiController]
    [Route("api/shortages")]
    public class ShortagesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ShortagesController(IMediator mediator) => _mediator = mediator;

        // Varsayılan Pending; status/projectId filtreli
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] ShortageStatus? status, [FromQuery] int? projectId)
            => Ok(await _mediator.Send(new GetShortagesQuery(status ?? ShortageStatus.Pending, projectId)));

        [HttpPost("dispatch")]
        public async Task<IActionResult> Dispatch([FromBody] DispatchShortagesCommand command)
            => Ok(new { shipmentIds = await _mediator.Send(command) });

        [HttpPost("{id:int}/cancel")]
        public async Task<IActionResult> Cancel(int id, [FromBody] CancelShortageRequest req)
        {
            await _mediator.Send(new CancelShortageCommand(id, req.Reason));
            return Ok();
        }
    }

    public record CancelShortageRequest(string Reason);
}
