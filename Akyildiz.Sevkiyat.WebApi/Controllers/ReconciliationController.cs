using Akyildiz.Sevkiyat.Application.Reconciliation.Commands.AcknowledgeReconciliationIssue;
using Akyildiz.Sevkiyat.Application.Reconciliation.Commands.RunReconciliationChecks;
using Akyildiz.Sevkiyat.Application.Reconciliation.Queries.CanShipmentProceed;
using Akyildiz.Sevkiyat.Application.Reconciliation.Queries.GetReconciliationIssues;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [ApiController]
    [Route("api/reconciliation")]
    [Authorize(Roles = "Admin,Manager")]
    public class ReconciliationController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ReconciliationController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Tüm operasyonel tutarsızlık kontrollerini çalıştırır.
        /// Yeni sorunları kayıt eder, çözülenlerini AutoResolved yapar.
        /// </summary>
        [HttpPost("run")]
        public async Task<IActionResult> RunChecks(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            CancellationToken ct)
        {
            var result = await _mediator.Send(new RunReconciliationChecksCommand(fromDate, toDate), ct);
            return Ok(result);
        }

        /// <summary>
        /// Tutarsızlık sorunlarını filtreli listeler.
        /// Varsayılan olarak AutoResolved kayıtlar gizlenir.
        /// </summary>
        [HttpGet("issues")]
        public async Task<IActionResult> GetIssues(
            [FromQuery] ReconciliationCheckType? checkType,
            [FromQuery] ReconciliationStatus?    status,
            [FromQuery] ReconciliationSeverity?  severity,
            [FromQuery] DateTime?                fromDate,
            [FromQuery] DateTime?                toDate,
            [FromQuery] int page     = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken ct = default)
        {
            var result = await _mediator.Send(
                new GetReconciliationIssuesQuery(checkType, status, severity, fromDate, toDate, page, pageSize), ct);
            return Ok(result);
        }

        /// <summary>
        /// Sevkiyat için hangi operasyonların engellendiğini döner.
        /// UI, bu yanıta göre butonları devre dışı bırakabilir.
        /// </summary>
        [HttpGet("can-proceed/{shipmentId:int}")]
        public async Task<IActionResult> CanProceed(int shipmentId, CancellationToken ct)
        {
            var result = await _mediator.Send(new CanShipmentProceedQuery(shipmentId), ct);
            return Ok(result);
        }

        /// <summary>
        /// Tutarsızlık kaydını "Acknowledged" olarak işaretler.
        /// Zorunlu: açıklama notu (ne yapıldı / neden kabul edildi).
        /// </summary>
        [HttpPost("issues/{id:int}/acknowledge")]
        public async Task<IActionResult> Acknowledge(int id, [FromBody] AcknowledgeRequest body, CancellationToken ct)
        {
            await _mediator.Send(new AcknowledgeReconciliationIssueCommand(id, body.Note), ct);
            return Ok(new { message = $"Sorun #{id} acknowledged olarak işaretlendi." });
        }
    }

    public record AcknowledgeRequest(string Note);
}
