using Akyildiz.Sevkiyat.Application.WarehouseLocations.Commands.BulkCreateWarehouseLocations;
using Akyildiz.Sevkiyat.Application.WarehouseLocations.Commands.CreateAreaLocation;
using Akyildiz.Sevkiyat.Application.WarehouseLocations.Commands.DeleteWarehouseLocation;
using Akyildiz.Sevkiyat.Application.WarehouseLocations.Commands.CreatePickingFace;
using Akyildiz.Sevkiyat.Application.WarehouseLocations.Commands.CreateWarehouseLocation;
using Akyildiz.Sevkiyat.Application.WarehouseLocations.Commands.UpdateWarehouseLocation;
using Akyildiz.Sevkiyat.Application.WarehouseLocations.Queries.GenerateLocationQr;
using Akyildiz.Sevkiyat.Application.WarehouseLocations.Queries.GetWarehouseLocations;
using Akyildiz.Sevkiyat.Application.WarehouseLocations.Queries.GetWarehouseMap;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/warehouse-locations")]
    public class WarehouseLocationsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public WarehouseLocationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager,Warehouse,Accounting")]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? koridorNo,
            [FromQuery] string? taraf,
            [FromQuery] LocationType? type,
            [FromQuery] bool includeInactive = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken ct = default)
        {
            var result = await _mediator.Send(
                new GetWarehouseLocationsQuery(koridorNo, taraf, type, includeInactive, page, pageSize), ct);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager,Accounting")]
        public async Task<IActionResult> Create(
            [FromBody] CreateWarehouseLocationCommand command,
            CancellationToken ct = default)
        {
            var id = await _mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetAll), new { }, new { id });
        }

        [HttpPost("bulk")]
        [Authorize(Roles = "Admin,Manager,Accounting")]
        public async Task<IActionResult> BulkCreate(
            [FromBody] BulkCreateWarehouseLocationsCommand command,
            CancellationToken ct = default)
        {
            var result = await _mediator.Send(command, ct);
            return Ok(result);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Manager,Accounting")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateWarehouseLocationRequest request,
            CancellationToken ct = default)
        {
            await _mediator.Send(new UpdateWarehouseLocationCommand(
                id,
                request.LocationType,
                request.Description,
                request.MaxWeightKg,
                request.MaxPallets,
                request.IsActive,
                request.Alan,
                request.QrCode,
                request.TotalFloors,
                request.ContainerType,
                request.InnerLevel,
                request.InnerPosition
            ), ct);
            return NoContent();
        }

        [HttpGet("map")]
        [Authorize(Roles = "Admin,Manager,Accounting,Warehouse")]
        public async Task<IActionResult> GetMap(CancellationToken ct = default)
        {
            var result = await _mediator.Send(new GetWarehouseMapQuery(), ct);
            return Ok(result);
        }

        [HttpPost("area")]
        [Authorize(Roles = "Admin,Manager,Accounting")]
        public async Task<IActionResult> CreateAreaLocation(
            [FromBody] CreateAreaLocationCommand command,
            CancellationToken ct = default)
        {
            var result = await _mediator.Send(command, ct);
            return Ok(result);
        }

        [HttpPost("picking-face")]
        [Authorize(Roles = "Admin,Manager,Accounting")]
        public async Task<IActionResult> CreatePickingFace(
            [FromBody] CreatePickingFaceCommand command,
            CancellationToken ct = default)
        {
            var result = await _mediator.Send(command, ct);
            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
        {
            await _mediator.Send(new DeleteWarehouseLocationCommand(id), ct);
            return NoContent();
        }

        [HttpDelete("bulk")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> BulkDelete([FromBody] List<int> ids, CancellationToken ct = default)
        {
            var deleted = await _mediator.Send(new BulkDeleteWarehouseLocationsCommand(ids), ct);
            return Ok(new { deleted });
        }

        [HttpGet("{id:int}/qr")]
        [Authorize(Roles = "Admin,Manager,Accounting,Warehouse")]
        public async Task<IActionResult> GetQr(int id, CancellationToken ct = default)
        {
            var result = await _mediator.Send(new GenerateLocationQrQuery(id), ct);
            return Ok(result);
        }
    }

    public record UpdateWarehouseLocationRequest(
        LocationType  LocationType,
        string?       Description,
        decimal?      MaxWeightKg,
        int?          MaxPallets,
        bool          IsActive,
        string?       Alan,
        string?       QrCode,
        int?          TotalFloors,
        ContainerType ContainerType = ContainerType.Pallet,
        string?       InnerLevel    = null,
        int?          InnerPosition = null
    );
}
