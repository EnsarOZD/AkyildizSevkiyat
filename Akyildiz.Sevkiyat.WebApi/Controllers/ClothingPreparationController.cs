using Akyildiz.Sevkiyat.Application.ClothingPreparation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [Authorize(Roles = "Admin,Manager,Accounting,Warehouse")]
    [ApiController]
    [Route("api/clothing-prep")]
    public class ClothingPreparationController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ClothingPreparationController(IMediator mediator) => _mediator = mediator;

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
            => Ok(await _mediator.Send(new GetClothingPrepDashboardQuery()));

        [HttpGet("{id:int}/pick-list")]
        public async Task<IActionResult> PickList(int id)
            => Ok(await _mediator.Send(new GetClothingPickListQuery(id)));

        [HttpPost("start")]
        public async Task<IActionResult> Start([FromBody] StartClothingPreparationRequest request)
            => Ok(new { count = await _mediator.Send(new StartClothingPreparationCommand(request.ShipmentIds)) });

        [HttpPost("{id:int}/complete")]
        public async Task<IActionResult> Complete(int id, [FromBody] CompleteClothingPrepRequest request)
        {
            await _mediator.Send(new CompleteClothingPreparationCommand(id, request.KoliCount, request.Lines ?? new()));
            return Ok();
        }
    }

    public record StartClothingPreparationRequest(List<int> ShipmentIds);
    public record CompleteClothingPrepRequest(string? KoliCount, List<ClothingPickLineDto>? Lines);
}
