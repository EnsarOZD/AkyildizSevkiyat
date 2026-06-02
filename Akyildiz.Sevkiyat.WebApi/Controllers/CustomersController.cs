using Akyildiz.Sevkiyat.Application.Customers.Commands.CreateManualCustomer;
using Akyildiz.Sevkiyat.Application.Customers.Commands.ToggleManualCustomerActive;
using Akyildiz.Sevkiyat.Application.Customers.Commands.UpdateManualCustomer;
using Akyildiz.Sevkiyat.Application.Customers.Queries.GetManualCustomerById;
using Akyildiz.Sevkiyat.Application.Customers.Queries.GetManualCustomers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CustomersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? search = null,
            [FromQuery] bool showInactive = false)
        {
            var result = await _mediator.Send(new GetManualCustomersQuery(pageNumber, pageSize, search, showInactive));
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _mediator.Send(new GetManualCustomerByIdQuery(id));
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateManualCustomerCommand command)
        {
            var id = await _mediator.Send(command);
            return Ok(new { Id = id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateManualCustomerCommand command)
        {
            if (id != command.Id) return BadRequest();
            await _mediator.Send(command);
            return NoContent();
        }

        [HttpPatch("{id}/toggle-active")]
        public async Task<IActionResult> ToggleActive(int id, [FromQuery] bool isActive)
        {
            await _mediator.Send(new ToggleManualCustomerActiveCommand(id, isActive));
            return NoContent();
        }
    }
}
