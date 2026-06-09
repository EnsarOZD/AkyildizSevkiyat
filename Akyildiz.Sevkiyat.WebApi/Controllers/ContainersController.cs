using Akyildiz.Sevkiyat.Application.ClothingPicking.Containers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [Authorize(Roles = "Admin,Manager,Accounting,Warehouse")]
    [ApiController]
    [Route("api/containers")]
    public class ContainersController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ContainersController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false)
            => Ok(await _mediator.Send(new GetContainersQuery(activeOnly)));

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Save([FromBody] SaveContainerCommand command)
            => Ok(new { id = await _mediator.Send(command) });

        // Soft delete — yalnızca pasifleştirir
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Deactivate(int id)
        {
            await _mediator.Send(new DeactivateContainerCommand(id));
            return Ok();
        }
    }
}
