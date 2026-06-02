using Akyildiz.Sevkiyat.Application.InstitutionCariMappings.Commands.ApplyMappingsToProjects;
using Akyildiz.Sevkiyat.Application.InstitutionCariMappings.Commands.CreateInstitutionCariMapping;
using Akyildiz.Sevkiyat.Application.InstitutionCariMappings.Commands.DeleteInstitutionCariMapping;
using Akyildiz.Sevkiyat.Application.InstitutionCariMappings.Commands.UpdateInstitutionCariMapping;
using Akyildiz.Sevkiyat.Application.InstitutionCariMappings.Queries.GetInstitutionCariMappings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/institution-cari-mappings")]
    public class InstitutionCariMappingsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public InstitutionCariMappingsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool showInactive = false)
        {
            var result = await _mediator.Send(new GetInstitutionCariMappingsQuery(showInactive));
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateInstitutionCariMappingCommand command)
        {
            var id = await _mediator.Send(command);
            return Ok(new { Id = id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateInstitutionCariMappingCommand command)
        {
            if (id != command.Id) return BadRequest();
            await _mediator.Send(command);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteInstitutionCariMappingCommand(id));
            return NoContent();
        }

        /// <summary>
        /// Tanımlı eşleşmeleri mevcut ISS projelerine uygular.
        /// dryRun=true → sadece etkilenecekleri listele.
        /// dryRun=false → değişiklikleri kaydet.
        /// </summary>
        [HttpPost("apply-to-projects")]
        public async Task<IActionResult> ApplyToProjects([FromQuery] bool dryRun = true)
        {
            var result = await _mediator.Send(new ApplyMappingsToProjectsCommand(dryRun));
            return Ok(result);
        }
    }
}
