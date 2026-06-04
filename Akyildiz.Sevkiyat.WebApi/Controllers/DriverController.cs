using Akyildiz.Sevkiyat.Application.Driver.Commands.EndDriverSession;
using Akyildiz.Sevkiyat.Application.Driver.Commands.StartDriverSession;
using Akyildiz.Sevkiyat.Application.Driver.Commands.UpdateRouteOrder;
using Akyildiz.Sevkiyat.Application.Driver.Queries.GetActiveDriverSession;
using Akyildiz.Sevkiyat.Application.Driver.Queries.GetDriverRoute;
using Akyildiz.Sevkiyat.Application.Driver.Queries.GetEndOdometerStatus;
using Akyildiz.Sevkiyat.Application.Driver.Queries.GetDriverShipments;
using Akyildiz.Sevkiyat.Application.Driver.Queries.ResolveIrsaliyeShipments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Manager,Accounting,Driver")]
    public class DriverController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DriverController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Eski endpoint — geriye dönük uyumluluk için korunuyor.
        /// Yeni şoför ekranı /route kullanır.
        /// </summary>
        [HttpGet("shipments")]
        public async Task<ActionResult<List<DriverShipmentDto>>> GetShipments(CancellationToken cancellationToken)
        {
            return await _mediator.Send(new GetDriverShipmentsQuery(), cancellationToken);
        }

        /// <summary>
        /// Şoför rota görünümü: teslimat noktaları (proje bazında gruplu), sıralı.
        /// </summary>
        [HttpGet("route")]
        public async Task<ActionResult<DriverRouteDto>> GetRoute(CancellationToken cancellationToken)
        {
            return await _mediator.Send(new GetDriverRouteQuery(), cancellationToken);
        }

        // ── Session Endpoints (Driver only) ───────────────────────────────────

        [HttpPost("sessions/start")]
        [Authorize(Roles = "Driver")]
        public async Task<ActionResult<StartDriverSessionResult>> StartSession(
            [FromBody] StartDriverSessionCommand command,
            CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(command, cancellationToken));
        }

        /// <summary>
        /// Sefer başlatmadan önce okutulan irsaliye QR'ından (no) o seferin sevkiyatlarını
        /// çözer — şoför listeyi görüp onaylar. Salt-okunur, durum değiştirmez.
        /// </summary>
        [HttpPost("sessions/resolve-irsaliye")]
        [Authorize(Roles = "Driver")]
        public async Task<ActionResult<ResolveIrsaliyeShipmentsResult>> ResolveIrsaliye(
            [FromBody] ResolveIrsaliyeShipmentsQuery query,
            CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(query, cancellationToken));
        }

        [HttpPost("sessions/end")]
        [Authorize(Roles = "Driver")]
        public async Task<ActionResult<EndDriverSessionResult>> EndSession(
            [FromBody] EndDriverSessionCommand command,
            CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(command, cancellationToken));
        }

        /// <summary>
        /// Sefer bitirmeden önce: bu araç için bugün başka bir şoför zaten bitiş
        /// kadranını girdi mi? Girdiyse şoföre kadran adımı sorulmaz. Salt-okunur.
        /// </summary>
        [HttpPost("sessions/end-odometer-status")]
        [Authorize(Roles = "Driver")]
        public async Task<ActionResult<EndOdometerStatusResult>> GetEndOdometerStatus(
            [FromBody] GetEndOdometerStatusQuery query,
            CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(query, cancellationToken));
        }

        [HttpGet("sessions/active")]
        [Authorize(Roles = "Driver")]
        public async Task<ActionResult<ActiveDriverSessionDto?>> GetActiveSession(
            CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(new GetActiveDriverSessionQuery(), cancellationToken));
        }

        /// <summary>
        /// Şoför rota sıralamasını günceller (ZonePreparationProject.RouteOrder).
        /// Project.DeliveryOrder'ı etkilemez.
        /// </summary>
        [HttpPut("route/reorder")]
        public async Task<IActionResult> ReorderRoute(
            [FromBody] UpdateDriverRouteOrderCommand command,
            CancellationToken cancellationToken)
        {
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }
    }
}
