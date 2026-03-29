using System.Net.Http.Json;
using System.Text.Json;
using Akyildiz.Sevkiyat.Application.RouteOptimization.Dtos;
using Akyildiz.Sevkiyat.Application.SystemSettings.Commands;
using Akyildiz.Sevkiyat.Application.SystemSettings.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

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

        public SystemSettingsController(ISender mediator, IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _mediator = mediator;
            _httpClientFactory = httpClientFactory;
            _config = config;
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
}
