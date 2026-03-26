using Akyildiz.Sevkiyat.Application.Dashboard.Queries.GetDashboardStats;
using Akyildiz.Sevkiyat.Application.Stocks.Queries.GetCriticalStocks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DashboardController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetDashboardStatsQuery(), cancellationToken);
            return Ok(result);
        }

        [HttpGet("critical-stocks")]
        [Authorize(Roles = "Admin,Manager,Warehouse")]
        public async Task<IActionResult> GetCriticalStocks(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetCriticalStocksQuery(), cancellationToken);
            return Ok(result);
        }
    }
}
