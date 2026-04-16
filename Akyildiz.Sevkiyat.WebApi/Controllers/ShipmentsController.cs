using Akyildiz.Sevkiyat.Application.Shipments.Commands;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.CreateShipment;
using Akyildiz.Sevkiyat.Application.Shipments.Queries;
using Akyildiz.Sevkiyat.Application.Shipments.Queries.GetShipmentsByDate;
using Akyildiz.Sevkiyat.Application.Shipments.Queries.GetShipments;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.UpdateShipmentLineDeliveredQty;
using Akyildiz.Sevkiyat.WebApi.Controllers.Models;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.MarkShipmentPreparing;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.MarkShipmentDelivered;
using Akyildiz.Sevkiyat.Application.Shipments.Queries.GetShipmentDetail;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.AssignToWarehouse;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.StartPicking;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.MarkReady;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.AssignVehicle;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.UpdateShipmentQuantities;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.ToggleShipmentStatus;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.UpdateShipmentDetails;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.UpdateIrsaliyeNo;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.RecordVehicleReturn;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.LogShipmentPrint;
using Microsoft.AspNetCore.Authorization;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ShipmentsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ShipmentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // POST api/shipments
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateShipmentCommand command)
        {
            var id = await _mediator.Send(command);
            // Şimdilik oluşan Shipment Id'yi döndürüyoruz
            return Ok(id);
        }

        [HttpPost("bulk")]
        public async Task<IActionResult> CreateBulk([FromBody] CreateBulkShipmentsCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetShipments([FromQuery] GetShipmentsQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("by-order/{orderId:int}")]
        public async Task<IActionResult> GetByOrderId(int orderId)
        {
            var result = await _mediator.Send(new GetShipmentsByOrderIdQuery(orderId));
            return Ok(result);
        }

        [HttpPatch("{id:int}/lines/{lineId:int}/delivered")]
        public async Task<IActionResult> UpdateDeliveredQty(
            int id,
            int lineId,
            [FromBody] UpdateDeliveredQtyRequest body)
        {
            await _mediator.Send(new UpdateShipmentLineDeliveredQtyCommand(
                ShipmentId: id,
                LineId: lineId,
                DeliveredQty: body.DeliveredQty,
                DifferenceReason: body.DifferenceReason,
                Note: body.Note
            ));

            return NoContent(); // 204
        }      

        [HttpPut("{id:int}/quantities")]
        [Authorize(Roles = "Admin,Warehouse,Manager")]
        public async Task<IActionResult> UpdateQuantities(int id, [FromBody] UpdateShipmentQuantitiesRequest request)
        {
            await _mediator.Send(new UpdateShipmentQuantitiesCommand(id, request.Lines));
            return NoContent();
        }

        [HttpPost("{id:int}/mark-preparing")]
        [Authorize(Roles = "Admin,Manager,Warehouse")]
        public async Task<IActionResult> MarkPreparing(int id)
        {
            await _mediator.Send(new MarkShipmentPreparingCommand(id));
            return NoContent();
        }

        [HttpPost("{id:int}/toggle-status")]
        [Authorize(Roles = "Admin,Accounting,Manager")]
        public async Task<IActionResult> ToggleStatus(int id, [FromQuery] bool setPassive, [FromBody] ChangeStatusRequest? request)
        {
            await _mediator.Send(new ToggleShipmentStatusCommand(id, setPassive, request?.Reason));
            return NoContent();
        }

        [HttpPut("{id:int}/details")]
        [Authorize(Roles = "Admin,Accounting,Manager")]
        public async Task<IActionResult> UpdateDetails(int id, [FromBody] UpdateShipmentDetailsCommand request)
        {
            if (id != request.ShipmentId) return BadRequest();
            await _mediator.Send(request);
            return NoContent();
        }

        [HttpPost("{id:int}/mark-delivered")]
        [Authorize(Roles = "Admin,Manager,Dispatcher,Driver")]
        public async Task<IActionResult> MarkDelivered(int id, [FromBody] MarkDeliveredRequest? request)
        {
            await _mediator.Send(new MarkShipmentDeliveredCommand(id, request?.DeliveryNote, request?.DeliveryRecipient, request?.DeliveryPhotoBase64, request?.OverrideNote));
            return NoContent();
        }

        [HttpPost("update-line")]
        [Authorize(Roles = "Admin,Warehouse,Manager")]
        public async Task<IActionResult> UpdateLine([FromBody] Akyildiz.Sevkiyat.Application.Shipments.Commands.UpdateShipmentLine.UpdateShipmentLineCommand command)
        {
            await _mediator.Send(command);
            return NoContent();
        }

        // WORKFLOW ENDPOINTS

        [HttpPost("{id:int}/assign-to-warehouse")]
        [Authorize(Roles = "Admin,Accounting,Manager")]
        public async Task<IActionResult> AssignToWarehouse(int id)
        {
            var result = await _mediator.Send(new AssignToWarehouseCommand(id));
            if (result.Warnings.Count > 0)
                return Ok(new { warnings = result.Warnings });
            return NoContent();
        }

        [HttpPost("{id:int}/start-picking")]
        [Authorize(Roles = "Admin,Warehouse,Manager")]
        public async Task<IActionResult> StartPicking(int id, [FromBody] ChangeStatusRequest? request)
        {
            await _mediator.Send(new StartPickingCommand(id, request?.Reason));
            return NoContent();
        }

        [HttpPost("{id:int}/revert-to-draft")]
        [Authorize(Roles = "Admin,Accounting,Manager")]
        public async Task<IActionResult> RevertToDraft(int id, [FromBody] RevertToDraftRequest request)
        {
            await _mediator.Send(new Akyildiz.Sevkiyat.Application.Shipments.Commands.RevertToDraft.RevertToDraftCommand(id, request.Reason));
            return NoContent();
        }

        [HttpPost("{id:int}/mark-ready")]
        [Authorize(Roles = "Admin,Warehouse,Manager")]
        public async Task<IActionResult> MarkReady(int id, [FromBody] ChangeStatusRequest? request)
        {
            var result = await _mediator.Send(new MarkReadyCommand(id, request?.Reason));
            if (result.Warnings.Count > 0)
                return Ok(new { warnings = result.Warnings });
            return NoContent();
        }

        [HttpPost("{id:int}/assign-vehicle")]
        [Authorize(Roles = "Admin,Dispatcher,Manager")]
        public async Task<IActionResult> AssignVehicle(int id, [FromBody] AssignVehicleRequest request)
        {
            var result = await _mediator.Send(new AssignVehicleCommand(id, request.DriverId, request.VehicleId));
            return Ok(result);
        }

        [HttpPut("{id:int}/irsaliye")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateIrsaliye(int id, [FromBody] UpdateIrsaliyeRequest request)
        {
            await _mediator.Send(new UpdateIrsaliyeNoCommand(id, request.IrsaliyeNo, request.IrsaliyeDate));
            return NoContent();
        }

        [HttpGet("{id:int}/detail")]
        [Authorize(Roles = "Admin,Manager,Dispatcher,Warehouse,Accounting,Driver")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var result = await _mediator.Send(new GetShipmentDetailQuery(id));
            return Ok(result);
        }

        [HttpPost("bulk-assign-vehicle")]
        [Authorize(Roles = "Admin,Dispatcher,Manager")]
        public async Task<IActionResult> BulkAssignVehicle([FromBody] BulkAssignVehicleRequest request)
        {
            var result = await _mediator.Send(new BulkAssignVehicleCommand(request.ShipmentIds, request.DriverId, request.VehicleId));
            return Ok(result);
        }

        [HttpPost("{id:int}/log-print")]
        [Authorize(Roles = "Admin,Manager,Dispatcher,Warehouse,Accounting")]
        public async Task<IActionResult> LogPrint(int id)
        {
            var result = await _mediator.Send(new LogShipmentPrintCommand(id));
            return Ok(result);
        }

        [HttpPost("{id:int}/record-vehicle-return")]
        [Authorize(Roles = "Admin,Manager,Warehouse,Dispatcher,Driver")]
        public async Task<IActionResult> RecordVehicleReturn(int id, [FromBody] RecordVehicleReturnRequest request)
        {
            var lines = request.Lines
                .Select(l => new ReturnLineDto(l.ShipmentLineId, l.ReturnedQty, l.ReturnReason))
                .ToList();
            await _mediator.Send(new RecordVehicleReturnCommand(id, lines, request.ReturnNote, request.OverrideNote));
            return NoContent();
        }
    }
}
