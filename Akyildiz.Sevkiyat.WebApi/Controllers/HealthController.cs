using Akyildiz.Sevkiyat.Infrastructure.BackgroundJobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Akyildiz.Sevkiyat.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class HealthController : ControllerBase
{
    private readonly BackgroundServiceStatusTracker _tracker;

    public HealthController(BackgroundServiceStatusTracker tracker)
    {
        _tracker = tracker;
    }

    [HttpGet("background-services")]
    public IActionResult GetBackgroundServices()
    {
        var statuses = _tracker.GetAll()
            .Select(kv => new
            {
                name = kv.Key,
                runAt = kv.Value.RunAt,
                result = kv.Value.Result.ToString(),
                errorMessage = kv.Value.ErrorMessage
            });
        return Ok(statuses);
    }
}
