using Akyildiz.Sevkiyat.Application.Users.Commands.CreateUser;
using Akyildiz.Sevkiyat.Application.Users.Commands.ResetPassword;
using Akyildiz.Sevkiyat.Application.Users.Commands.ToggleUserActive;
using Akyildiz.Sevkiyat.Application.Users.Commands.UpdateUser;
using Akyildiz.Sevkiyat.Application.Users.Queries.GetUsers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UsersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _mediator.Send(new GetUsersQuery());
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserCommand command)
        {
            var id = await _mediator.Send(command);
            return Ok(id);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserCommand command)
        {
            if (id != command.Id) return BadRequest("ID uyumsuzluğu.");
            await _mediator.Send(command);
            return NoContent();
        }

        [HttpPost("{id:int}/toggle-active")]
        public async Task<IActionResult> ToggleActive(int id, [FromBody] ToggleActiveRequest request)
        {
            await _mediator.Send(new ToggleUserActiveCommand(id, request.IsActive));
            return NoContent();
        }

        [HttpPost("{id:int}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordRequest request)
        {
            await _mediator.Send(new ResetPasswordCommand(id, request.NewPassword));
            return NoContent();
        }
    }

    public record ToggleActiveRequest(bool IsActive);
    public record ResetPasswordRequest(string NewPassword);
}
