using Akyildiz.Sevkiyat.Application.FreightDeliveries.Commands.SubmitFreightDeliveryProof;
using Akyildiz.Sevkiyat.Application.FreightDeliveries.Queries.GetFreightDeliveryByToken;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    /// <summary>
    /// Nakliyecinin login olmadan (token'lı link ile) teslim fotoğrafı yüklediği public uçlar.
    /// </summary>
    [AllowAnonymous]
    [ApiController]
    [Route("api/public/freight-delivery")]
    public class FreightDeliveryController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FreightDeliveryController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{token}")]
        public async Task<ActionResult<FreightDeliveryInfoDto>> Get(string token, CancellationToken ct)
        {
            return Ok(await _mediator.Send(new GetFreightDeliveryByTokenQuery(token), ct));
        }

        public record SubmitProofRequest(string RecipientName, string? Note, List<string> PhotosBase64);

        [HttpPost("{token}/proof")]
        public async Task<ActionResult<SubmitFreightDeliveryProofResult>> SubmitProof(
            string token, [FromBody] SubmitProofRequest body, CancellationToken ct)
        {
            var result = await _mediator.Send(new SubmitFreightDeliveryProofCommand(
                token, body.RecipientName, body.Note, body.PhotosBase64 ?? new List<string>()), ct);
            return Ok(result);
        }
    }
}
