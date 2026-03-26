using System;
using System.Threading.Tasks;
using Akyildiz.Sevkiyat.Application.Orders.Queries;
using Akyildiz.Sevkiyat.Application.Orders.Queries.GetOrdersByDeliveryDate;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public OrdersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET api/orders/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _mediator.Send(new GetOrderByIdQuery(id));
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // GET api/orders/by-order-date?date=2025-12-10
        [HttpGet("by-order-date")]
        public async Task<IActionResult> GetByOrderDate([FromQuery] DateTime date)
        {
            var result = await _mediator.Send(new GetOrdersByDateQuery(date));
            return Ok(result);
        }

        // GET api/orders/by-delivery-date?date=2025-12-10
        [HttpGet("by-delivery-date")]
        public async Task<IActionResult> GetByDeliveryDate([FromQuery] DateTime date)
        {
            var result = await _mediator.Send(
                new GetOrdersByDeliveryDateQuery(date)
            );

            return Ok(result);
        }
    }
}
