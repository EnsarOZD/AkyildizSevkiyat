using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.RouteOptimization.Dtos;
using Akyildiz.Sevkiyat.Application.RouteOptimization.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [ApiController]
    [Route("api/route-optimization")]
    [Authorize]
    public class RouteOptimizationController : ControllerBase
    {
        private readonly IIssSyncComparisonService _comparisonService;
        private readonly IRouteOptimizationService _routeService;
        private readonly IApplicationDbContext _context;
        private readonly IISSIpClient _issClient;

        public RouteOptimizationController(
            IIssSyncComparisonService comparisonService,
            IRouteOptimizationService routeService,
            IApplicationDbContext context,
            IISSIpClient issClient)
        {
            _comparisonService = comparisonService;
            _routeService      = routeService;
            _context           = context;
            _issClient         = issClient;
        }

        // POST /api/route-optimization/compare
        [HttpPost("compare")]
        public async Task<IActionResult> Compare(
            [FromBody] CompareRequest body,
            CancellationToken ct)
        {
            if (body.ProjectCodes == null || body.ProjectCodes.Count == 0)
                return BadRequest("En az bir proje kodu gerekli.");

            var result = await _comparisonService.CompareWithIssAsync(body.ProjectCodes, ct);
            return Ok(result);
        }

        public record CompareRequest(List<string> ProjectCodes);

        // POST /api/route-optimization/sync
        [HttpPost("sync")]
        public async Task<IActionResult> Sync(
            [FromBody] List<SyncApprovalRequestDto> approvals,
            CancellationToken ct)
        {
            if (approvals == null || approvals.Count == 0)
                return BadRequest("Onay listesi boş.");

            var codes = approvals.Select(a => a.ProjectCode).ToList();
            var projects = await _context.Projects
                .Where(p => codes.Contains(p.Code))
                .ToListAsync(ct);

            var opts = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            int updated = 0;

            foreach (var approval in approvals)
            {
                if (!approval.ApproveNameUpdate && !approval.ApproveAddressUpdate) continue;

                var project = projects.FirstOrDefault(p => p.Code == approval.ProjectCode);
                if (project == null) continue;

                try
                {
                    var envelope = await _issClient.GetProjeAsync(project.Code, ct);
                    var root = GetRefinedRoot(envelope.Root);

                    Application.External.IssIp.Dtos.IssProjeDto? dto = null;
                    if (root.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        if (TryGetProperty(root, out var tbl, "Table", "Proje", "Data", "Result"))
                        {
                            var list = System.Text.Json.JsonSerializer.Deserialize<
                                List<Application.External.IssIp.Dtos.IssProjeDto>>(tbl.GetRawText(), opts);
                            dto = list?.FirstOrDefault();
                        }
                        else
                        {
                            try { dto = System.Text.Json.JsonSerializer.Deserialize<
                                Application.External.IssIp.Dtos.IssProjeDto>(root.GetRawText(), opts); } catch { }
                        }
                    }
                    else if (root.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        var list = System.Text.Json.JsonSerializer.Deserialize<
                            List<Application.External.IssIp.Dtos.IssProjeDto>>(root.GetRawText(), opts);
                        dto = list?.FirstOrDefault();
                    }

                    if (dto == null) continue;

                    if (approval.ApproveNameUpdate && !string.IsNullOrWhiteSpace(dto.ProjeAdi))
                        project.Name = dto.ProjeAdi;

                    if (approval.ApproveAddressUpdate && !string.IsNullOrWhiteSpace(dto.MalzemeTeslimAdresi))
                        project.Address = dto.MalzemeTeslimAdresi;

                    project.LastSyncedAt = DateTime.UtcNow;
                    updated++;
                }
                catch { /* hata loglama eklenmeli — controller seviyesinde basit devam */ }
            }

            await _context.SaveChangesAsync(ct);
            return Ok(new { Updated = updated });
        }

        // POST /api/route-optimization/optimize
        [HttpPost("optimize")]
        [EnableRateLimiting("optimize")]
        public async Task<IActionResult> Optimize(
            [FromBody] RouteOptimizationRequestDto body,
            CancellationToken ct)
        {
            if (body.ProjectCodes == null || body.ProjectCodes.Count == 0)
                return BadRequest("En az bir proje kodu gerekli.");

            var projects = await _context.Projects
                .Where(p => body.ProjectCodes.Contains(p.Code))
                .ToListAsync(ct);

            // Preserve input order
            var addresses = body.ProjectCodes
                .Select(code => projects.FirstOrDefault(p => p.Code == code)?.Address ?? "")
                .ToList();

            var result = await _routeService.OptimizeRouteAsync(
                addresses, body.StartAddress, body.ProjectCodes, ct);

            return Ok(result);
        }

        private System.Text.Json.JsonElement GetRefinedRoot(System.Text.Json.JsonElement root)
        {
            if (root.ValueKind == System.Text.Json.JsonValueKind.Object &&
                TryGetProperty(root, out var rp, "result", "Result") &&
                rp.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var inner = rp.GetString();
                if (!string.IsNullOrWhiteSpace(inner))
                {
                    try
                    {
                        var doc = System.Text.Json.JsonDocument.Parse(inner);
                        return doc.RootElement.Clone();
                    }
                    catch { }
                }
            }
            return root;
        }

        private bool TryGetProperty(System.Text.Json.JsonElement el, out System.Text.Json.JsonElement val, params string[] names)
        {
            val = default;
            foreach (var n in names) if (el.TryGetProperty(n, out val)) return true;
            return false;
        }
    }
}
