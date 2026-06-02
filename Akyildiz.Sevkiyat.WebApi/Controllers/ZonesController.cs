using Akyildiz.Sevkiyat.Application.Zones.Commands.CreateZone;
using Akyildiz.Sevkiyat.Application.Zones.Commands.DeleteZone;
using Akyildiz.Sevkiyat.Application.Zones.Commands.SetZoneActive;
using Akyildiz.Sevkiyat.Application.Zones.Commands.UpdateZone;
using Akyildiz.Sevkiyat.Application.Zones.Queries.GetZones;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ZonesController : ControllerBase
    {
        private readonly ISender _mediator;

        public ZonesController(ISender mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<List<ZoneDto>> Get([FromQuery] bool includeInactive = false)
        {
            return await _mediator.Send(new GetZonesQuery(includeInactive));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager,Accounting")]
        public async Task<int> Create([FromBody] CreateZoneCommand command)
        {
            return await _mediator.Send(command);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Manager,Accounting")]
        public async Task<IActionResult> Update(int id, UpdateZoneCommand command)
        {
            if (id != command.Id) return BadRequest();
            await _mediator.Send(command);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Manager,Accounting")]
        public async Task<IActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteZoneCommand(id));
            return NoContent();
        }

        [HttpPatch("{id}/active")]
        [Authorize(Roles = "Admin,Manager,Accounting")]
        public async Task<IActionResult> SetActive(int id, [FromQuery] bool isActive)
        {
            await _mediator.Send(new SetZoneActiveCommand(id, isActive));
            return NoContent();
        }
    }
}
