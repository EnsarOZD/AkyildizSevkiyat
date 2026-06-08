using Akyildiz.Sevkiyat.Application.ClothingKeywords;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/clothing-keywords")]
    public class ClothingKeywordsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ClothingKeywordsController(IMediator mediator) => _mediator = mediator;

        // Toplama ekranı aktif kelimeleri okuyabilsin diye GET tüm rollere açık.
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false)
            => Ok(await _mediator.Send(new GetClothingKeywordsQuery(activeOnly)));

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Save([FromBody] SaveClothingKeywordCommand command)
            => Ok(new { id = await _mediator.Send(command) });

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteClothingKeywordCommand(id));
            return Ok();
        }
    }
}
