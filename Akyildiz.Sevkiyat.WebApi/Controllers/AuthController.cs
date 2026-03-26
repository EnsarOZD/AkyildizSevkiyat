using Akyildiz.Sevkiyat.Application.Auth.Commands.Login;
using Akyildiz.Sevkiyat.Application.Auth.Commands.RefreshToken;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new RefreshTokenCommand(), cancellationToken);
            return Ok(result);
        }
    }
}
