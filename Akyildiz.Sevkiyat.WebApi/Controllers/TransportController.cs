using Akyildiz.Sevkiyat.Application.Transport.Drivers.Commands.CreateDriver;
using Akyildiz.Sevkiyat.Application.Transport.Drivers.Commands.DeleteDriver;
using Akyildiz.Sevkiyat.Application.Transport.Drivers.Commands.UpdateDriver;
using Akyildiz.Sevkiyat.Application.Transport.Drivers.Queries.GetDrivers;
using Akyildiz.Sevkiyat.Application.Transport.Vehicles.Commands.CreateVehicle;
using Akyildiz.Sevkiyat.Application.Transport.Vehicles.Commands.DeleteVehicle;
using Akyildiz.Sevkiyat.Application.Transport.Vehicles.Commands.UpdateVehicle;
using Akyildiz.Sevkiyat.Application.Transport.Vehicles.Queries.GetVehicles;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransportController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TransportController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // --- DRIVERS ---

        [HttpGet("drivers")]
        public async Task<ActionResult<List<DriverDto>>> GetDrivers()
        {
            return await _mediator.Send(new GetDriversQuery());
        }

        [HttpPost("drivers")]
        public async Task<ActionResult<int>> CreateDriver([FromBody] CreateDriverCommand command)
        {
            return await _mediator.Send(command);
        }

        [HttpPut("drivers")]
        public async Task<ActionResult<bool>> UpdateDriver([FromBody] UpdateDriverCommand command)
        {
            return await _mediator.Send(command);
        }

        [HttpDelete("drivers/{id}")]
        public async Task<ActionResult<bool>> DeleteDriver(int id)
        {
            return await _mediator.Send(new DeleteDriverCommand(id));
        }

        // --- VEHICLES ---

        [HttpGet("vehicles")]
        public async Task<ActionResult<List<VehicleDto>>> GetVehicles()
        {
            return await _mediator.Send(new GetVehiclesQuery());
        }

        [HttpPost("vehicles")]
        public async Task<ActionResult<int>> CreateVehicle([FromBody] CreateVehicleCommand command)
        {
            return await _mediator.Send(command);
        }

        [HttpPut("vehicles")]
        public async Task<ActionResult<bool>> UpdateVehicle([FromBody] UpdateVehicleCommand command)
        {
            return await _mediator.Send(command);
        }

        [HttpDelete("vehicles/{id}")]
        public async Task<ActionResult<bool>> DeleteVehicle(int id)
        {
            return await _mediator.Send(new DeleteVehicleCommand(id));
        }
    }
}
