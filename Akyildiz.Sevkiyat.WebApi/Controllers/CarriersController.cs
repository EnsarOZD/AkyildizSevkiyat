using Akyildiz.Sevkiyat.Application.Carriers.Commands.CreateCarrier;
using Akyildiz.Sevkiyat.Application.Carriers.Commands.DeleteCarrier;
using Akyildiz.Sevkiyat.Application.Carriers.Commands.UpdateCarrier;
using Akyildiz.Sevkiyat.Application.Carriers.Queries.GetCarriers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/carriers")]
    public class CarriersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CarriersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? search, [FromQuery] bool? isActive)
        {
            var result = await _mediator.Send(new GetCarriersQuery { Search = search, IsActive = isActive });
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager,Accounting")]
        public async Task<IActionResult> Create(CreateCarrierCommand command)
        {
            var id = await _mediator.Send(command);
            return Ok(new { Id = id });
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Manager,Accounting")]
        public async Task<IActionResult> Update(int id, UpdateCarrierCommand command)
        {
            command.Id = id;
            await _mediator.Send(command);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteCarrierCommand(id));
            return NoContent();
        }
    }
}
