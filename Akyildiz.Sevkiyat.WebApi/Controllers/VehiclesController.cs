using Akyildiz.Sevkiyat.Application.Vehicles.Commands.GenerateVehicleQrCode;
using Akyildiz.Sevkiyat.Application.Vehicles.Queries.GetVehicleQrImage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [ApiController]
    [Route("api/vehicles")]
    [Authorize]
    public class VehiclesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public VehiclesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("{vehicleId:int}/generate-qr")]
        [Authorize(Roles = "Admin,Manager,Accounting")]
        public async Task<IActionResult> GenerateQr(int vehicleId, CancellationToken ct)
        {
            var result = await _mediator.Send(new GenerateVehicleQrCodeCommand(vehicleId), ct);
            return Ok(result);
        }

        [HttpGet("{vehicleId:int}/qr-image")]
        [Authorize(Roles = "Admin,Manager,Accounting,Warehouse")]
        public async Task<IActionResult> GetQrImage(int vehicleId, CancellationToken ct)
        {
            var result = await _mediator.Send(new GetVehicleQrImageQuery(vehicleId), ct);
            return Ok(result);
        }
    }
}
