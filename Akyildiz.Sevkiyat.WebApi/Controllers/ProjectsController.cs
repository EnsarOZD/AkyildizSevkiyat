using Akyildiz.Sevkiyat.Application.Projects.Commands.ImportProjectMappings;
using Akyildiz.Sevkiyat.Application.Projects.Queries;
using Akyildiz.Sevkiyat.Application.Projects.Queries.ExportProjectMappings;
using Akyildiz.Sevkiyat.Application.Projects.Commands.CreateProject;
using Akyildiz.Sevkiyat.Application.Projects.Commands.UpdateProject;
using Akyildiz.Sevkiyat.Application.Projects.Commands.DeleteProject;
using Akyildiz.Sevkiyat.Application.Projects.Commands.SyncProjects;
using Akyildiz.Sevkiyat.Application.Projects.Commands.UpdateProjectZone;
using Akyildiz.Sevkiyat.Application.Projects.Commands.UpdateProjectNetsisCariKodu;
using Akyildiz.Sevkiyat.Application.Projects.Commands.UpdateProjectLocation;
using Akyildiz.Sevkiyat.Application.Projects.Commands.UpdateProjectDeliveryOrder;
using Akyildiz.Sevkiyat.Application.Projects.Commands.BulkUpdateDeliveryOrders;
using Akyildiz.Sevkiyat.Application.Projects.Commands.UpdateProjectDeliveryWindow;
using Akyildiz.Sevkiyat.Application.Projects.Commands.ToggleProjectActive;
using Akyildiz.Sevkiyat.Application.Projects.Queries.ValidateProjectCoordinates;
using Akyildiz.Sevkiyat.Application.Projects.Commands.UpdateProjectContact;
using Akyildiz.Sevkiyat.Application.Projects.Queries.GetProjectContacts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ProjectsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? search = null,
            [FromQuery] bool showInactive = false)
        {
            var result = await _mediator.Send(new GetAllProjectsQuery(pageNumber, pageSize, search, showInactive));
            return Ok(result);
        }

        [HttpGet("export-mappings")]
        public async Task<IActionResult> ExportMappings()
        {
            var result = await _mediator.Send(new ExportProjectMappingsQuery());
            return File(result.Content, result.ContentType, result.FileName);
        }

        [HttpPost("import-mappings")]
        [RequestSizeLimit(20 * 1024 * 1024)]   // 20 MB
        [RequestFormLimits(MultipartBodyLengthLimit = 20 * 1024 * 1024)]
        public async Task<IActionResult> ImportMappings(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Dosya seçilmedi.");
            using var stream = file.OpenReadStream();
            var result = await _mediator.Send(new ImportProjectMappingsCommand(stream));
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateProjectCommand command)
        {
            var id = await _mediator.Send(command);
            return Ok(id);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateProjectCommand command)
        {
            if (id != command.Id) return BadRequest();
            await _mediator.Send(command);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteProjectCommand(id));
            return NoContent();
        }

        [HttpPatch("{id}/toggle-active")]
        public async Task<IActionResult> ToggleActive(int id, [FromQuery] bool isActive)
        {
            await _mediator.Send(new ToggleProjectActiveCommand(id, isActive));
            return NoContent();
        }
        [HttpPatch("{id}/zone")]
        public async Task<IActionResult> UpdateZone(int id, [FromBody] UpdateZoneBody body)
        {
            await _mediator.Send(new UpdateProjectZoneCommand(id, body.ZoneId));
            return NoContent();
        }

        public record UpdateZoneBody(int ZoneId);

        [HttpPatch("{id}/netsis-cari-kodu")]
        public async Task<IActionResult> UpdateNetsisCariKodu(int id, [FromBody] UpdateNetsisCariKoduBody body)
        {
            await _mediator.Send(new UpdateProjectNetsisCariKoduCommand(id, body.NetsisCariKodu, body.NetsisTeslimCariKodu));
            return NoContent();
        }

        public record UpdateNetsisCariKoduBody(string? NetsisCariKodu, string? NetsisTeslimCariKodu = null);

        [HttpPost("sync")]
        public async Task<IActionResult> Sync([FromBody] SyncProjectsCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(new { Count = result.SyncedCount, result.AddressChanges });
        }

        [HttpPatch("{id}/location")]
        public async Task<IActionResult> UpdateLocation(int id, [FromBody] UpdateLocationBody body)
        {
            // Kaynak: body'de belirtilmişse onu kullan; yoksa şoför ise "doğrulandı" kabul et.
            Akyildiz.Sevkiyat.Domain.Enums.LocationSource? source = null;
            if (!string.IsNullOrWhiteSpace(body.Source)
                && System.Enum.TryParse<Akyildiz.Sevkiyat.Domain.Enums.LocationSource>(body.Source, true, out var parsed))
            {
                source = parsed;
            }
            else if (User.IsInRole("Driver"))
            {
                source = Akyildiz.Sevkiyat.Domain.Enums.LocationSource.DriverVerified;
            }

            await _mediator.Send(new UpdateProjectLocationCommand(id, body.Latitude, body.Longitude, body.CityName, body.DistrictName, source));
            return NoContent();
        }

        public record UpdateLocationBody(double? Latitude, double? Longitude, string? CityName = null, string? DistrictName = null, string? Source = null);

        [HttpPost("validate-coordinates")]
        public async Task<IActionResult> ValidateCoordinates([FromBody] ValidateCoordinatesBody body)
        {
            var result = await _mediator.Send(new ValidateProjectCoordinatesQuery(body.ProjectIds));
            return Ok(result);
        }

        public record ValidateCoordinatesBody(List<int> ProjectIds);

        [HttpPatch("{id}/delivery-order")]
        public async Task<IActionResult> UpdateDeliveryOrder(int id, [FromBody] UpdateDeliveryOrderBody body)
        {
            await _mediator.Send(new UpdateProjectDeliveryOrderCommand(id, body.DeliveryOrder));
            return NoContent();
        }

        public record UpdateDeliveryOrderBody(int? DeliveryOrder);

        [HttpPatch("{id}/delivery-window")]
        public async Task<IActionResult> UpdateDeliveryWindow(int id, [FromBody] UpdateDeliveryWindowBody body)
        {
            TimeOnly? start = string.IsNullOrWhiteSpace(body.DeliveryWindowStart)
                ? null : TimeOnly.Parse(body.DeliveryWindowStart);
            TimeOnly? end = string.IsNullOrWhiteSpace(body.DeliveryWindowEnd)
                ? null : TimeOnly.Parse(body.DeliveryWindowEnd);
            await _mediator.Send(new UpdateProjectDeliveryWindowCommand(id, start, end));
            return NoContent();
        }

        public record UpdateDeliveryWindowBody(string? DeliveryWindowStart, string? DeliveryWindowEnd);

        [HttpPatch("bulk-delivery-orders")]
        public async Task<IActionResult> BulkUpdateDeliveryOrders([FromBody] BulkUpdateDeliveryOrdersCommand command)
        {
            await _mediator.Send(command);
            return NoContent();
        }

        /// <summary>PATCH /api/projects/batch-delivery-order — rota sıralaması için toplu DeliveryOrder güncelleme.</summary>
        [HttpPatch("batch-delivery-order")]
        public async Task<IActionResult> BatchDeliveryOrder([FromBody] BulkUpdateDeliveryOrdersCommand command)
        {
            await _mediator.Send(command);
            return NoContent();
        }

        [HttpGet("contacts")]
        public async Task<IActionResult> GetContacts()
        {
            var result = await _mediator.Send(new GetProjectContactsQuery());
            return Ok(result);
        }

        [HttpPatch("{id}/contact")]
        public async Task<IActionResult> UpdateContact(int id, [FromBody] UpdateContactBody body)
        {
            await _mediator.Send(new UpdateProjectContactCommand(id, body.ContactName, body.ContactPhone));
            return NoContent();
        }

        public record UpdateContactBody(string? ContactName, string? ContactPhone);
    }
}
