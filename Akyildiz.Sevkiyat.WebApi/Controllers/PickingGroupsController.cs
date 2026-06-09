using Akyildiz.Sevkiyat.Application.ClothingPicking.PickingGroups;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [Authorize(Roles = "Admin,Manager,Accounting,Warehouse")]
    [ApiController]
    [Route("api/picking-groups")]
    public class PickingGroupsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public PickingGroupsController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false)
            => Ok(await _mediator.Send(new GetPickingGroupsQuery(activeOnly)));

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Save([FromBody] SavePickingGroupCommand command)
            => Ok(new { id = await _mediator.Send(command) });

        // Soft delete — yalnızca pasifleştirir (hard delete yok)
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Deactivate(int id)
        {
            await _mediator.Send(new DeactivatePickingGroupCommand(id));
            return Ok();
        }
    }
}
