using Akyildiz.Sevkiyat.Application.ExternalEmailContacts.Commands;
using Akyildiz.Sevkiyat.Application.ExternalEmailContacts.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [Authorize(Roles = "Admin,Manager,Accounting")]
    [ApiController]
    [Route("api/external-email-contacts")]
    public class ExternalEmailContactsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ExternalEmailContactsController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool? activeOnly)
        {
            var result = await _mediator.Send(new GetExternalEmailContactsQuery(activeOnly));
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateExternalEmailContactCommand command)
        {
            var id = await _mediator.Send(command);
            return Ok(new { id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateExternalEmailContactCommand command)
        {
            if (id != command.Id) return BadRequest("ID mismatch");
            await _mediator.Send(command);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteExternalEmailContactCommand(id));
            return Ok();
        }
    }
}
