using Akyildiz.Sevkiyat.Application.ClothingPicking;
using Akyildiz.Sevkiyat.Domain.Enums;
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

        // ── Toplama akışı ───────────────────────────────────────────────────
        [HttpPost("{id:int}/mode")]
        public async Task<IActionResult> SetMode(int id, [FromBody] SetModeRequest req)
        {
            await _mediator.Send(new SetPickingModeCommand(id, req.Mode));
            return Ok();
        }

        [HttpGet("{id:int}/containers")]
        public async Task<IActionResult> Containers(int id)
            => Ok(await _mediator.Send(new GetShipmentContainersQuery(id)));

        [HttpPost("{id:int}/scan-container")]
        public async Task<IActionResult> ScanContainer(int id, [FromBody] ScanContainerRequest req)
            => Ok(await _mediator.Send(new ScanContainerCommand(id, req.Code)));

        [HttpPost("release-container")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> ReleaseContainer([FromBody] ReleaseContainerCommand command)
        {
            await _mediator.Send(command);
            return Ok();
        }

        [HttpPost("{id:int}/save-progress")]
        public async Task<IActionResult> SaveProgress(int id, [FromBody] SavePickProgressRequest req)
        {
            await _mediator.Send(new SaveClothingPickProgressCommand(id, req.Lines ?? new()));
            return Ok();
        }

        [HttpPost("{id:int}/complete-picking")]
        public async Task<IActionResult> CompletePicking(int id, [FromBody] CompletePickingRequest req)
        {
            await _mediator.Send(new CompletePickingCommand(id, req.Lines ?? new(), req.ConfirmContainers, req.PalletCount));
            return Ok();
        }

        [HttpPost("{id:int}/pause")]
        public async Task<IActionResult> Pause(int id)
        {
            await _mediator.Send(new PausePickingCommand(id));
            return Ok();
        }

        // ── Kapama + etiket ─────────────────────────────────────────────────
        [HttpPost("{id:int}/complete-closing")]
        public async Task<IActionResult> CompleteClosing(int id, [FromBody] CompleteClosingRequest req)
        {
            await _mediator.Send(new CompleteClosingCommand(id, req.BoxCount, req.PackageType, req.Note));
            return Ok();
        }

        [HttpPost("{id:int}/label-handwritten")]
        public async Task<IActionResult> LabelHandwritten(int id)
        {
            await _mediator.Send(new MarkLabelHandwrittenCommand(id));
            return Ok();
        }

        [HttpPost("{id:int}/resume")]
        public async Task<IActionResult> Resume(int id)
        {
            await _mediator.Send(new ResumePickingCommand(id));
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
    public record SetModeRequest(PickingMode Mode);
    public record ScanContainerRequest(string Code);
    public record SavePickProgressRequest(List<ClothingPickLineInput>? Lines);
    public record CompletePickingRequest(List<ClothingPickLineInput>? Lines, bool ConfirmContainers, int? PalletCount);
    public record CompleteClosingRequest(int BoxCount, PackageType PackageType, string? Note);
}
