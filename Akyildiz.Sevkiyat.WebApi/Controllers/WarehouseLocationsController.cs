using Akyildiz.Sevkiyat.Application.WarehouseLocations.Commands.BulkCreateWarehouseLocations;
using Akyildiz.Sevkiyat.Application.WarehouseLocations.Commands.CreateWarehouseLocation;
using Akyildiz.Sevkiyat.Application.WarehouseLocations.Commands.UpdateWarehouseLocation;
using Akyildiz.Sevkiyat.Application.WarehouseLocations.Queries.GetWarehouseLocations;
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
                request.IsActive
            ), ct);
            return NoContent();
        }
    }

    public record UpdateWarehouseLocationRequest(
        LocationType LocationType,
        string? Description,
        decimal? MaxWeightKg,
        int? MaxPallets,
        bool IsActive
    );
}
