using Akyildiz.Sevkiyat.Application.DefinedReasons.Commands;
using Akyildiz.Sevkiyat.Application.DefinedReasons.Queries;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/defined-reasons")]
    public class DefinedReasonsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public DefinedReasonsController(IMediator mediator) => _mediator = mediator;

        // Okuma: tüm authenticated kullanıcılar (picking → depo, red → muhasebe/depo kullanır).
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] ReasonCategory? category, [FromQuery] bool? activeOnly)
        {
            var result = await _mediator.Send(new GetDefinedReasonsQuery(category, activeOnly));
            return Ok(result);
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDefinedReasonCommand command)
        {
            var id = await _mediator.Send(command);
            return Ok(new { id });
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateDefinedReasonCommand command)
        {
            if (id != command.Id) return BadRequest("ID mismatch");
            await _mediator.Send(command);
            return Ok();
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteDefinedReasonCommand(id));
            return Ok();
        }
    }
}
