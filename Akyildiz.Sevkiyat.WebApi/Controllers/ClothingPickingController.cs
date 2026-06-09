using Akyildiz.Sevkiyat.Application.ClothingPicking;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [Authorize(Roles = "Admin,Manager,Accounting,Warehouse")]
    [ApiController]
    [Route("api/clothing-picking")]
    public class ClothingPickingController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ClothingPickingController(IMediator mediator) => _mediator = mediator;

        // ── Kuyruk ──────────────────────────────────────────────────────────
        [HttpGet("queue")]
        public async Task<IActionResult> Queue([FromQuery] int? groupId)
            => Ok(await _mediator.Send(new GetPickingQueueQuery(groupId)));

        // ── Claim / Unclaim ─────────────────────────────────────────────────
        [HttpPost("{id:int}/claim")]
        public async Task<IActionResult> Claim(int id)
        {
            await _mediator.Send(new ClaimShipmentCommand(id));
            return Ok();
        }

        [HttpPost("{id:int}/unclaim")]
        public async Task<IActionResult> Unclaim(int id, [FromBody] UnclaimRequest req)
        {
            await _mediator.Send(new UnclaimShipmentCommand(id, req.Reason));
            return Ok();
        }

        // ── Yönetici: gruplama / sıralama / rezervasyon ─────────────────────
        [HttpPost("assign-group")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> AssignGroup([FromBody] AssignShipmentsToGroupCommand command)
            => Ok(new { count = await _mediator.Send(command) });

        [HttpPost("reorder")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Reorder([FromBody] ReorderPickingQueueCommand command)
        {
            await _mediator.Send(command);
            return Ok();
        }

        [HttpPost("{id:int}/reserve")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Reserve(int id, [FromBody] ReserveRequest req)
        {
            await _mediator.Send(new ReserveShipmentForPickerCommand(id, req.ReservedForUserId));
            return Ok();
        }
    }

    public record UnclaimRequest(string Reason);
    public record ReserveRequest(int? ReservedForUserId);
}
