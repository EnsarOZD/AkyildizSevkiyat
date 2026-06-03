using Akyildiz.Sevkiyat.Application.FreightDeliveries.Queries.GetActiveFreightDeliveries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    /// <summary>
    /// Nakliyeci teslim linklerinin yönetimi (listeleme/tekrar gönderme) — yetkili kullanıcılar.
    /// Public yükleme uçları için FreightDeliveryController'a bakın.
    /// </summary>
    [ApiController]
    [Route("api/freight-deliveries")]
    [Authorize(Roles = "Admin,Manager,Accounting,Warehouse")]
    public class FreightDeliveriesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FreightDeliveriesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult<List<FreightDeliveryListItemDto>>> Get(
            [FromQuery] bool includeInactive = false, CancellationToken ct = default)
        {
            return Ok(await _mediator.Send(new GetActiveFreightDeliveriesQuery(includeInactive), ct));
        }
    }
}
