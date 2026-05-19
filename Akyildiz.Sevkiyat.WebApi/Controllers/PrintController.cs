using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [ApiController]
    [Route("api/print")]
    public class PrintController : ControllerBase
    {
        private readonly IApplicationDbContext _context;

        public PrintController(IApplicationDbContext context)
        {
            _context = context;
        }

        // ── Agent Endpoints (AgentKey auth) ─────────────────────────────────────

        /// <summary>
        /// Agent başlarken veya heartbeat olarak çağırır.
        /// Kendini kaydeder, kurulu yazıcıları bildirir.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("agents/register")]
        public async Task<IActionResult> RegisterAgent([FromBody] RegisterAgentRequest req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.AgentKey))
                return BadRequest(new { message = "AgentKey zorunludur." });

            var agent = await _context.PrintAgents
                .FirstOrDefaultAsync(a => a.AgentKey == req.AgentKey, ct);

            if (agent == null)
            {
                agent = new PrintAgent
                {
                    AgentKey     = req.AgentKey,
                    MachineName  = req.MachineName,
                    DisplayName  = req.DisplayName ?? req.MachineName,
                    CreatedAt    = DateTime.UtcNow,
                };
                _context.PrintAgents.Add(agent);
            }
            else
            {
                agent.MachineName = req.MachineName;
                if (!string.IsNullOrWhiteSpace(req.DisplayName))
                    agent.DisplayName = req.DisplayName;
            }

            agent.LastSeenAt           = DateTime.UtcNow;
            agent.InstalledPrintersJson = req.InstalledPrinters != null
                ? JsonSerializer.Serialize(req.InstalledPrinters)
                : agent.InstalledPrintersJson;

            await _context.SaveChangesAsync(ct);

            return Ok(new { agentId = agent.Id, displayName = agent.DisplayName });
        }

        /// <summary>Agent kendi PrinterConfig'lerindeki bekleyen işleri çeker.</summary>
        [AllowAnonymous]
        [HttpGet("jobs/pending")]
        public async Task<IActionResult> GetPendingJobs([FromQuery] string agentKey, CancellationToken ct)
        {
            var agent = await _context.PrintAgents
                .FirstOrDefaultAsync(a => a.AgentKey == agentKey, ct);

            if (agent == null) return Unauthorized(new { message = "Geçersiz AgentKey." });

            // Heartbeat
            agent.LastSeenAt = DateTime.UtcNow;

            var jobs = await _context.PrintJobs
                .Include(j => j.PrinterConfig)
                .Where(j =>
                    j.Status == PrintJobStatus.Pending &&
                    j.PrinterConfig.AgentId == agent.Id &&
                    j.PrinterConfig.IsActive)
                .OrderBy(j => j.CreatedAt)
                .Take(5)
                .Select(j => new
                {
                    j.Id,
                    j.LabelType,
                    j.PayloadJson,
                    PrinterName = j.PrinterConfig.WindowsPrinterName,
                })
                .ToListAsync(ct);

            await _context.SaveChangesAsync(ct);

            return Ok(jobs);
        }

        /// <summary>Agent bir işin durumunu günceller (Printing / Done / Failed).</summary>
        [AllowAnonymous]
        [HttpPatch("jobs/{id}/status")]
        public async Task<IActionResult> UpdateJobStatus(int id, [FromBody] UpdateJobStatusRequest req,
            [FromQuery] string agentKey, CancellationToken ct)
        {
            var agent = await _context.PrintAgents
                .FirstOrDefaultAsync(a => a.AgentKey == agentKey, ct);
            if (agent == null) return Unauthorized();

            var job = await _context.PrintJobs
                .Include(j => j.PrinterConfig)
                .FirstOrDefaultAsync(j => j.Id == id && j.PrinterConfig.AgentId == agent.Id, ct);
            if (job == null) return NotFound();

            job.Status       = req.Status;
            job.ErrorMessage = req.ErrorMessage;

            if (req.Status == PrintJobStatus.Printing) job.StartedAt   = DateTime.UtcNow;
            if (req.Status is PrintJobStatus.Done or PrintJobStatus.Failed) job.CompletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            return Ok();
        }

        // ── Admin / UI Endpoints ─────────────────────────────────────────────────

        /// <summary>Kayıtlı agent listesi.</summary>
        [Authorize(Roles = "Admin,Manager")]
        [HttpGet("agents")]
        public async Task<IActionResult> GetAgents(CancellationToken ct)
        {
            var agents = await _context.PrintAgents
                .Include(a => a.PrinterConfigs)
                .OrderByDescending(a => a.LastSeenAt)
                .Select(a => new
                {
                    a.Id,
                    a.AgentKey,
                    a.MachineName,
                    a.DisplayName,
                    a.LastSeenAt,
                    IsOnline = a.LastSeenAt >= DateTime.UtcNow.AddSeconds(-15),
                    InstalledPrinters = a.InstalledPrintersJson != null
                        ? JsonSerializer.Deserialize<List<string>>(a.InstalledPrintersJson)
                        : new List<string>(),
                    PrinterConfigCount = a.PrinterConfigs.Count(c => c.IsActive),
                })
                .ToListAsync(ct);

            return Ok(agents);
        }

        /// <summary>Yazıcı konfigürasyonları.</summary>
        [Authorize(Roles = "Admin,Manager")]
        [HttpGet("printer-configs")]
        public async Task<IActionResult> GetPrinterConfigs(CancellationToken ct)
        {
            var configs = await _context.PrinterConfigs
                .Include(c => c.Agent)
                .Where(c => c.IsActive)
                .OrderBy(c => c.LabelType)
                .ThenBy(c => c.Name)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.LabelType,
                    LabelTypeName    = c.LabelType == LabelType.CargoLabel ? "Kargo Etiketi" : "Koli Etiketi",
                    c.WindowsPrinterName,
                    c.IsDefault,
                    c.AgentId,
                    AgentDisplayName = c.Agent != null ? c.Agent.DisplayName : null,
                    AgentOnline      = c.Agent != null && c.Agent.LastSeenAt >= DateTime.UtcNow.AddSeconds(-15),
                    c.CreatedAt,
                })
                .ToListAsync(ct);

            return Ok(configs);
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost("printer-configs")]
        public async Task<IActionResult> CreatePrinterConfig([FromBody] PrinterConfigRequest req, CancellationToken ct)
        {
            if (req.IsDefault)
                await ClearDefault(req.LabelType, null, ct);

            var config = new PrinterConfig
            {
                Name               = req.Name,
                LabelType          = req.LabelType,
                WindowsPrinterName = req.WindowsPrinterName,
                AgentId            = req.AgentId,
                IsDefault          = req.IsDefault,
                IsActive           = true,
            };
            _context.PrinterConfigs.Add(config);
            await _context.SaveChangesAsync(ct);
            return Ok(new { id = config.Id });
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPut("printer-configs/{id}")]
        public async Task<IActionResult> UpdatePrinterConfig(int id, [FromBody] PrinterConfigRequest req, CancellationToken ct)
        {
            var config = await _context.PrinterConfigs.FindAsync(new object[] { id }, ct);
            if (config == null) return NotFound();

            if (req.IsDefault)
                await ClearDefault(req.LabelType, id, ct);

            config.Name               = req.Name;
            config.LabelType          = req.LabelType;
            config.WindowsPrinterName = req.WindowsPrinterName;
            config.AgentId            = req.AgentId;
            config.IsDefault          = req.IsDefault;

            await _context.SaveChangesAsync(ct);
            return Ok();
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpDelete("printer-configs/{id}")]
        public async Task<IActionResult> DeletePrinterConfig(int id, CancellationToken ct)
        {
            var config = await _context.PrinterConfigs.FindAsync(new object[] { id }, ct);
            if (config == null) return NotFound();
            config.IsActive = false;
            await _context.SaveChangesAsync(ct);
            return Ok();
        }

        /// <summary>Test baskısı — seçili yazıcıya örnek etiket gönderir.</summary>
        [Authorize(Roles = "Admin,Manager")]
        [HttpPost("printer-configs/{id}/test")]
        public async Task<IActionResult> TestPrint(int id, CancellationToken ct)
        {
            var config = await _context.PrinterConfigs
                .Include(c => c.Agent)
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive, ct);
            if (config == null) return NotFound();

            var payload = config.LabelType == LabelType.CargoLabel
                ? JsonSerializer.Serialize(new CargoLabelPayload
                {
                    Barcode      = "TEST12345678",
                    ReceiverName = "TEST - Akyıldız Şantiyesi",
                    Address      = "Test Adresi, İstanbul",
                    Phone        = "05001234567",
                    IrsaliyeNo   = "TEST/2026/001",
                    ShipmentId   = 0,
                    DeliveryDate = DateTime.Today.ToString("dd.MM.yyyy"),
                })
                : JsonSerializer.Serialize(new BoxLabelPayload
                {
                    ProjectName = "TEST PROJESİ",
                    Location    = "İSTANBUL",
                    ProjectCode = "99999",
                    BoxCount    = 1,
                });

            var job = new PrintJob
            {
                PrinterConfigId = config.Id,
                LabelType       = config.LabelType,
                PayloadJson     = payload,
                Status          = PrintJobStatus.Pending,
            };
            _context.PrintJobs.Add(job);
            await _context.SaveChangesAsync(ct);

            return Ok(new { jobId = job.Id, message = "Test baskısı kuyruğa alındı." });
        }

        /// <summary>Kargo etiketi baskı işi oluşturur.</summary>
        [Authorize]
        [HttpPost("jobs/cargo")]
        public async Task<IActionResult> CreateCargoJob([FromBody] CreateCargoJobRequest req, CancellationToken ct)
        {
            var config = req.PrinterConfigId.HasValue
                ? await _context.PrinterConfigs
                    .FirstOrDefaultAsync(c => c.Id == req.PrinterConfigId && c.IsActive, ct)
                : await _context.PrinterConfigs
                    .FirstOrDefaultAsync(c => c.LabelType == LabelType.CargoLabel && c.IsDefault && c.IsActive, ct);

            if (config == null)
                return BadRequest(new { message = "Aktif kargo etiketi yazıcısı bulunamadı. Lütfen yazıcı ayarlarını kontrol edin." });

            var payload = JsonSerializer.Serialize(req.Payload);

            var job = new PrintJob
            {
                PrinterConfigId  = config.Id,
                LabelType        = LabelType.CargoLabel,
                PayloadJson      = payload,
                Status           = PrintJobStatus.Pending,
            };
            _context.PrintJobs.Add(job);
            await _context.SaveChangesAsync(ct);

            return Ok(new { jobId = job.Id, message = "Baskı kuyruğa alındı." });
        }

        /// <summary>Tek bir baskı işinin güncel durumunu döner (polling için).</summary>
        [Authorize]
        [HttpGet("jobs/{id:int}")]
        public async Task<IActionResult> GetJob(int id, CancellationToken ct)
        {
            var job = await _context.PrintJobs
                .Include(j => j.PrinterConfig)
                .Where(j => j.Id == id)
                .Select(j => new
                {
                    j.Id,
                    j.LabelType,
                    j.Status,
                    StatusName    = j.Status.ToString(),
                    PrinterName   = j.PrinterConfig.Name,
                    j.ErrorMessage,
                    j.CreatedAt,
                    j.CompletedAt,
                })
                .FirstOrDefaultAsync(ct);

            if (job == null) return NotFound();
            return Ok(job);
        }

        /// <summary>Baskı kuyruğu — admin izleme.</summary>
        [Authorize(Roles = "Admin,Manager,Warehouse,Dispatcher")]
        [HttpGet("jobs")]
        public async Task<IActionResult> GetJobs([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
        {
            var query = _context.PrintJobs
                .Include(j => j.PrinterConfig)
                .OrderByDescending(j => j.CreatedAt);

            var total = await query.CountAsync(ct);
            var jobs  = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(j => new
                {
                    j.Id,
                    j.LabelType,
                    j.Status,
                    StatusName       = j.Status.ToString(),
                    PrinterName      = j.PrinterConfig.Name,
                    j.ErrorMessage,
                    j.CreatedAt,
                    j.CompletedAt,
                })
                .ToListAsync(ct);

            return Ok(new { total, items = jobs });
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        private async Task ClearDefault(LabelType type, int? excludeId, CancellationToken ct)
        {
            var existing = await _context.PrinterConfigs
                .Where(c => c.LabelType == type && c.IsDefault && (excludeId == null || c.Id != excludeId))
                .ToListAsync(ct);
            foreach (var c in existing)
                c.IsDefault = false;
        }
    }

    // ── Request / Response DTOs ──────────────────────────────────────────────────

    public record RegisterAgentRequest(
        string AgentKey,
        string MachineName,
        string? DisplayName,
        List<string>? InstalledPrinters
    );

    public record UpdateJobStatusRequest(PrintJobStatus Status, string? ErrorMessage);

    public record PrinterConfigRequest(
        string Name,
        LabelType LabelType,
        string WindowsPrinterName,
        int? AgentId,
        bool IsDefault
    );

    public record CreateCargoJobRequest(int? PrinterConfigId, CargoLabelPayload Payload);

    public record CargoLabelPayload
    {
        public string Barcode      { get; init; } = string.Empty;
        public string ReceiverName { get; init; } = string.Empty;
        public string Address      { get; init; } = string.Empty;
        public string Phone        { get; init; } = string.Empty;
        public string? IrsaliyeNo { get; init; }
        public int ShipmentId     { get; init; }
        public string DeliveryDate { get; init; } = string.Empty;
    }

    public record BoxLabelPayload
    {
        public string ProjectName { get; init; } = string.Empty;
        public string Location    { get; init; } = string.Empty;
        public string ProjectCode { get; init; } = string.Empty;
        public int BoxCount       { get; init; }
    }
}
