using Akyildiz.Sevkiyat.Application.Suppliers.Commands.CreateSupplier;
using Akyildiz.Sevkiyat.Application.Suppliers.Commands.ImportSuppliers;
using Akyildiz.Sevkiyat.Application.Suppliers.Queries.GetSuppliers;
using Akyildiz.Sevkiyat.Application.Suppliers.Queries.GetSuppliersTemplate;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/suppliers")]
    public class SuppliersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SuppliersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] GetSuppliersQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateSupplierCommand command)
        {
            var id = await _mediator.Send(command);
            return Ok(new { Id = id });
        }

        [HttpGet("template")]
        public async Task<IActionResult> Template()
        {
            var result = await _mediator.Send(new GetSuppliersTemplateQuery());
            return File(result.Content, result.ContentType, result.FileName);
        }

        [HttpPost("import")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Dosya yüklenmedi.");

            using var stream = file.OpenReadStream();
            var count = await _mediator.Send(new ImportSuppliersCommand(stream));
            return Ok(new { Count = count });
        }
    }
}
