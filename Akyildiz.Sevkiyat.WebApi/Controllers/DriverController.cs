using Akyildiz.Sevkiyat.Application.Driver.Queries.GetDriverRoute;
using Akyildiz.Sevkiyat.Application.Driver.Queries.GetDriverShipments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Manager,Dispatcher,Driver")]
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
    }
}
