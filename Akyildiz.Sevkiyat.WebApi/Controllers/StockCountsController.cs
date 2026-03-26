using Akyildiz.Sevkiyat.Application.StockCounts.Commands.CancelStockCount;
using Akyildiz.Sevkiyat.Application.StockCounts.Commands.CompleteStockCount;
using Akyildiz.Sevkiyat.Application.StockCounts.Commands.CreateStockCount;
using Akyildiz.Sevkiyat.Application.StockCounts.Commands.UpdateStockCountLines;
using Akyildiz.Sevkiyat.Application.StockCounts.Queries.GetStockCountDetail;
using Akyildiz.Sevkiyat.Application.StockCounts.Queries.GetStockCounts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class StockCountsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public StockCountsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager,Warehouse")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _mediator.Send(new GetStockCountsQuery());
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin,Manager,Warehouse")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var result = await _mediator.Send(new GetStockCountDetailQuery(id));
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create([FromBody] CreateStockCountRequest request)
        {
            var id = await _mediator.Send(new CreateStockCountCommand(request.CountDate, request.Note));
            return CreatedAtAction(nameof(GetDetail), new { id }, new { id });
        }

        [HttpPut("{id:int}/lines")]
        [Authorize(Roles = "Admin,Manager,Warehouse")]
        public async Task<IActionResult> UpdateLines(int id, [FromBody] UpdateStockCountLinesRequest request)
        {
            var lines = request.Lines
                .Select(l => new CountLineUpdateDto(l.StockCountLineId, l.ActualQty, l.Note))
                .ToList();
            await _mediator.Send(new UpdateStockCountLinesCommand(id, lines));
            return NoContent();
        }

        [HttpPost("{id:int}/complete")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Complete(int id)
        {
            var result = await _mediator.Send(new CompleteStockCountCommand(id));
            return Ok(result);
        }

        [HttpPost("{id:int}/cancel")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Cancel(int id)
        {
            await _mediator.Send(new CancelStockCountCommand(id));
            return NoContent();
        }

        [HttpGet("{id:int}/export")]
        [Authorize(Roles = "Admin,Manager,Warehouse")]
        public async Task<IActionResult> ExportTemplate(int id)
        {
            var excelData = await _mediator.Send(new Akyildiz.Sevkiyat.Application.StockCounts.Queries.ExportStockCountTemplate.ExportStockCountTemplateQuery(id));
            return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Sayim_Sablon_{id}.xlsx");
        }

        [HttpPost("{id:int}/import")]
        [Authorize(Roles = "Admin,Manager,Warehouse")]
        public async Task<IActionResult> ImportExcel(int id, Microsoft.AspNetCore.Http.IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Message = "Lütfen bir Excel dosyası yükleyin." });

            byte[] fileContent;
            using (var ms = new System.IO.MemoryStream())
            {
                await file.CopyToAsync(ms);
                fileContent = ms.ToArray();
            }

            var result = await _mediator.Send(new Akyildiz.Sevkiyat.Application.StockCounts.Commands.ImportStockCountFromExcel.ImportStockCountFromExcelCommand 
            { 
                StockCountId = id, 
                FileContent = fileContent 
            });

            return Ok(result);
        }
    }

    public record CreateStockCountRequest(DateTime CountDate, string? Note);

    public record UpdateStockCountLinesRequest(List<LineUpdateItem> Lines);
    public record LineUpdateItem(int StockCountLineId, decimal ActualQty, string? Note);
}
