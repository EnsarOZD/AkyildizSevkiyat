using System.Net.Http.Json;
using System.Text.Json;
using Akyildiz.Sevkiyat.Application.RouteOptimization.Dtos;
using Akyildiz.Sevkiyat.Application.SystemSettings.Commands;
using Akyildiz.Sevkiyat.Application.SystemSettings.Queries;
using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [ApiController]
    [Route("api/system-settings")]
    [Authorize]
    public class SystemSettingsController : ControllerBase
    {
        private readonly ISender _mediator;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly IApplicationDbContext _context;

        public SystemSettingsController(ISender mediator, IHttpClientFactory httpClientFactory, IConfiguration config, IApplicationDbContext context)
        {
            _mediator = mediator;
            _httpClientFactory = httpClientFactory;
            _config = config;
            _context = context;
        }

        [HttpGet("depot")]
        public async Task<IActionResult> GetDepot(CancellationToken ct)
        {
            var result = await _mediator.Send(new GetDepotSettingsQuery(), ct);
            return Ok(result);
        }

        [HttpPut("depot")]
        public async Task<IActionResult> SaveDepot([FromBody] SaveDepotSettingsCommand command, CancellationToken ct)
        {
            var result = await _mediator.Send(command, ct);
            return Ok(result);
        }

        // GET /api/system-settings/po-counter
        [HttpGet("po-counter")]
        [Authorize(Roles = "Admin,Manager,Accounting")]
        public async Task<IActionResult> GetPoCounters(CancellationToken ct)
        {
            var counters = await _context.PurchaseOrderNumberCounters
                .OrderByDescending(c => c.Year).ThenByDescending(c => c.Month)
                .Select(c => new
                {
                    c.Id,
                    c.Year,
                    c.Month,
                    c.LastValue,
                    FormattedNumber = $"{c.Year}{c.Month:D2}{c.LastValue:D7}",
                    NextNumber = $"{c.Year}{c.Month:D2}{(c.LastValue + 1):D7}"
                })
                .ToListAsync(ct);
            return Ok(counters);
        }

        // PUT /api/system-settings/po-counter/{id}
        [HttpPut("po-counter/{id:int}")]
        [Authorize(Roles = "Admin,Manager,Accounting")]
        public async Task<IActionResult> UpdatePoCounter(int id, [FromBody] UpdatePoCounterRequest request, CancellationToken ct)
        {
            var counter = await _context.PurchaseOrderNumberCounters
                .FirstOrDefaultAsync(c => c.Id == id, ct);

            if (counter == null)
                return NotFound();

            if (request.LastValue < 0)
                return BadRequest("Sayaç değeri negatif olamaz.");

            counter.LastValue = request.LastValue;
            await _context.SaveChangesAsync(ct);

            return Ok(new
            {
                counter.Id,
                counter.Year,
                counter.Month,
                counter.LastValue,
                FormattedNumber = $"{counter.Year}{counter.Month:D2}{counter.LastValue:D7}",
                NextNumber = $"{counter.Year}{counter.Month:D2}{(counter.LastValue + 1):D7}"
            });
        }

        // GET /api/system-settings/geocode?address=...
        [HttpGet("geocode")]
        public async Task<IActionResult> Geocode([FromQuery] string address, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(address))
                return BadRequest("address parametresi gerekli.");

            var apiKey = _config["GoogleMaps:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                return StatusCode(500, "GoogleMaps:ApiKey yapılandırması eksik.");

            var url = $"https://maps.googleapis.com/maps/api/geocode/json" +
                      $"?address={Uri.EscapeDataString(address)}&key={apiKey}&language=tr&region=tr";

            using var client = _httpClientFactory.CreateClient();
            using var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return StatusCode(502, "Geocoding API erişilemiyor.");

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var root = doc.RootElement;
            var status = root.GetProperty("status").GetString();

            if (status != "OK")
                return Ok(new { latitude = (double?)null, longitude = (double?)null });

            var loc = root.GetProperty("results")[0].GetProperty("geometry").GetProperty("location");
            return Ok(new
            {
                latitude  = loc.GetProperty("lat").GetDouble(),
                longitude = loc.GetProperty("lng").GetDouble()
            });
        }
    }

    public record UpdatePoCounterRequest(int LastValue);
}
