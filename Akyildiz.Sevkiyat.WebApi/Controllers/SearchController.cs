using Akyildiz.Sevkiyat.Application.Search.Queries.GlobalSearch;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly ISender _mediator;

        public SearchController(ISender mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Global search across shipments, stocks and projects.
        /// Minimum 2 characters required.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<GlobalSearchResultDto>> Search([FromQuery] string q = "")
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Ok(new GlobalSearchResultDto());

            var result = await _mediator.Send(new GetGlobalSearchQuery(q.Trim()));
            return Ok(result);
        }
    }
}
