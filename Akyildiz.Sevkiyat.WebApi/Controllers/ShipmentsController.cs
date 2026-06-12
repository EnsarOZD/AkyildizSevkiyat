using Akyildiz.Sevkiyat.Application.Shipments.Commands;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.BulkDispatchAsCargo;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.BulkDispatchAsFreight;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.CreateShipment;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.CreateManualShipment;
using Akyildiz.Sevkiyat.Application.Shipments.Queries;
using Akyildiz.Sevkiyat.Application.Shipments.Queries.GetShipmentsByDate;
using Akyildiz.Sevkiyat.Application.Shipments.Queries.GetShipments;
using Akyildiz.Sevkiyat.Application.Shipments.Queries.GetYkCargoReport;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.UpdateShipmentLineDeliveredQty;
using Akyildiz.Sevkiyat.WebApi.Controllers.Models;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.MarkShipmentPreparing;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.MarkShipmentDelivered;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.BulkMarkDelivered;
using Akyildiz.Sevkiyat.Application.Shipments.Queries.GetShipmentDetail;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.AssignToWarehouse;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.StartPicking;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.MarkReady;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.AssignVehicle;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.UpdateShipmentQuantities;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.ToggleShipmentStatus;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.CancelShipment;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.DeliverStop;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.UpdateShipmentDetails;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.UpdateIrsaliyeNo;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.RecordVehicleReturn;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.LogShipmentPrint;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.AdminResetShipment;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.DeleteDraftShipment;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.RevertDelivered;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.LogMissingItemsMail;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.SendComparisonEmail;
using Akyildiz.Sevkiyat.Application.Shipments.Queries.GetShipmentComparisonReport;
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

        // POST api/shipments/manual — manuel müşteri sevkiyatı
        [HttpPost("manual")]
        public async Task<IActionResult> CreateManual([FromBody] CreateManualShipmentCommand command)
        {
            var id = await _mediator.Send(command);
            return Ok(new { Id = id });
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

        [HttpGet("comparison-report")]
        [Authorize(Roles = "Admin,Manager,Accounting")]
        public async Task<IActionResult> GetComparisonReport([FromQuery] GetShipmentComparisonReportQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("yk-cargo-report")]
        public async Task<IActionResult> GetYkCargoReport([FromQuery] GetYkCargoReportQuery query)
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

        // POST api/shipments/stops/{projectId}/deliver — durak (proje) bazlı toplu teslim + iade
        [HttpPost("stops/{projectId:int}/deliver")]
        [Authorize(Roles = "Admin,Accounting,Manager,Driver")]
        public async Task<IActionResult> DeliverStop(int projectId, [FromBody] DeliverStopRequest request)
        {
            var result = await _mediator.Send(new DeliverStopCommand(
                projectId,
                request.DeliveryRecipient,
                request.DeliveryNote,
                request.PhotosBase64,
                request.Latitude,
                request.Longitude,
                request.Lines ?? new List<DeliverStopLineInput>(),
                request.ExternalReturns));
            return Ok(result);
        }

        // POST api/shipments/{id}/cancel — sebep girilerek iptal (pasife alma) + koşullu bildirim e-postası
        [HttpPost("{id:int}/cancel")]
        [Authorize(Roles = "Admin,Accounting,Manager")]
        public async Task<IActionResult> Cancel(int id, [FromBody] CancelShipmentRequest request)
        {
            var result = await _mediator.Send(
                new CancelShipmentCommand(id, request.Reason, request.NotifyOutOfStock, request.ExtraCc));
            return Ok(result);
        }

        [HttpPut("{id:int}/details")]
        [Authorize(Roles = "Admin,Accounting,Manager")]
        public async Task<IActionResult> UpdateDetails(int id, [FromBody] UpdateShipmentDetailsCommand request)
        {
            if (id != request.ShipmentId) return BadRequest();
            var result = await _mediator.Send(request);
            return Ok(result);
        }

        [HttpPost("{id:int}/mark-delivered")]
        [Authorize(Roles = "Admin,Manager,Accounting,Driver")]
        public async Task<IActionResult> MarkDelivered(int id, [FromBody] MarkDeliveredRequest? request)
        {
            await _mediator.Send(new MarkShipmentDeliveredCommand(id, request?.DeliveryNote, request?.DeliveryRecipient, request?.DeliveryPhotosBase64, request?.OverrideNote, request?.DeliveryLatitude, request?.DeliveryLongitude));
            return NoContent();
        }

        [HttpPost("bulk-mark-delivered")]
        [Authorize(Roles = "Admin,Manager,Accounting")]
        public async Task<IActionResult> BulkMarkDelivered([FromBody] BulkMarkShipmentsDeliveredCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
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
        [Authorize(Roles = "Admin,Warehouse,Manager,Accounting")]
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

        [HttpDelete("{id:int}/draft")]
        [Authorize(Roles = "Admin,Manager,Accounting")]
        public async Task<IActionResult> DeleteDraft(int id)
        {
            await _mediator.Send(new DeleteDraftShipmentCommand(id));
            return NoContent();
        }

        [HttpPost("{id:int}/mark-ready")]
        [Authorize(Roles = "Admin,Warehouse,Manager,Accounting")]
        public async Task<IActionResult> MarkReady(int id, [FromBody] ChangeStatusRequest? request)
        {
            var result = await _mediator.Send(new MarkReadyCommand(id, request?.Reason));
            if (result.Warnings.Count > 0)
                return Ok(new { warnings = result.Warnings });
            return NoContent();
        }

        [HttpPost("{id:int}/assign-vehicle")]
        [Authorize(Roles = "Admin,Manager,Accounting")]
        public async Task<IActionResult> AssignVehicle(int id, [FromBody] AssignVehicleRequest request)
        {
            var result = await _mediator.Send(new AssignVehicleCommand(id, request.DriverId, request.VehicleId));
            return Ok(result);
        }

        [HttpPut("{id:int}/irsaliye")]
        [Authorize(Roles = "Admin,Manager,Accounting")]
        public async Task<IActionResult> UpdateIrsaliye(int id, [FromBody] UpdateIrsaliyeRequest request)
        {
            await _mediator.Send(new UpdateIrsaliyeNoCommand(id, request.IrsaliyeNo, request.IrsaliyeDate));
            return NoContent();
        }

        [HttpGet("{id:int}/detail")]
        [Authorize(Roles = "Admin,Manager,Warehouse,Accounting,Driver")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var result = await _mediator.Send(new GetShipmentDetailQuery(id));
            return Ok(result);
        }

        [HttpPost("bulk-assign-vehicle")]
        [Authorize(Roles = "Admin,Manager,Accounting")]
        public async Task<IActionResult> BulkAssignVehicle([FromBody] BulkAssignVehicleRequest request)
        {
            var result = await _mediator.Send(new BulkAssignVehicleCommand(request.ShipmentIds, request.DriverId, request.VehicleId));
            return Ok(result);
        }

        [HttpPost("bulk-dispatch-cargo")]
        [Authorize(Roles = "Admin,Manager,Accounting")]
        public async Task<IActionResult> BulkDispatchAsCargo([FromBody] BulkDispatchAsCargoRequest request)
        {
            var result = await _mediator.Send(new BulkDispatchShipmentsAsCargoCommand
            {
                ShipmentIds = request.ShipmentIds,
                CargoProvider = (Domain.Enums.CargoProvider)request.CargoProvider,
                CargoTrackingNumber = request.CargoTrackingNumber,
            });
            return Ok(result);
        }

        [HttpPost("bulk-dispatch-freight")]
        [Authorize(Roles = "Admin,Manager,Accounting")]
        public async Task<IActionResult> BulkDispatchAsFreight([FromBody] BulkDispatchAsFreightRequest request)
        {
            var result = await _mediator.Send(new BulkDispatchShipmentsAsFreightCommand
            {
                ShipmentIds  = request.ShipmentIds,
                CarrierName  = request.CarrierName,
                CarrierPlate = request.CarrierPlate,
                CarrierPhone = request.CarrierPhone,
            });
            return Ok(result);
        }

        [HttpPost("{id:int}/log-print")]
        [Authorize(Roles = "Admin,Manager,Warehouse,Accounting")]
        public async Task<IActionResult> LogPrint(int id)
        {
            var result = await _mediator.Send(new LogShipmentPrintCommand(id));
            return Ok(result);
        }

        [HttpPost("{id:int}/admin-reset")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> AdminReset(int id, [FromBody] AdminResetRequest request)
        {
            await _mediator.Send(new AdminResetShipmentCommand(id, request.Reason));
            return NoContent();
        }

        [HttpPost("{id:int}/log-missing-mail")]
        [Authorize(Roles = "Admin,Manager,Accounting")]
        public async Task<IActionResult> LogMissingMail(int id)
        {
            await _mediator.Send(new LogMissingItemsMailCommand(id));
            return NoContent();
        }

        [HttpPost("{id:int}/send-comparison-email")]
        [Authorize(Roles = "Admin,Manager,Accounting,Dispatcher")]
        public async Task<IActionResult> SendComparisonEmail(int id, [FromBody] SendComparisonEmailRequest? request = null)
        {
            var sentTo = await _mediator.Send(new SendComparisonEmailCommand(id, request?.CcEmails));
            return Ok(new { sentTo });
        }

        [HttpPost("{id:int}/revert-delivered")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RevertDelivered(int id, [FromBody] RevertDeliveredRequest request)
        {
            await _mediator.Send(new RevertDeliveredCommand(id, request.Reason));
            return NoContent();
        }

        [HttpPost("{id:int}/record-vehicle-return")]

        [Authorize(Roles = "Admin,Manager,Accounting,Warehouse,Driver")]
        public async Task<IActionResult> RecordVehicleReturn(int id, [FromBody] RecordVehicleReturnRequest request)
        {
            var lines = request.Lines
                .Select(l => new ReturnLineDto(l.ShipmentLineId, l.ReturnedQty, l.ReturnReason))
                .ToList();
            await _mediator.Send(new RecordVehicleReturnCommand(id, lines, request.ReturnNote, request.OverrideNote));
            return NoContent();
        }

        [HttpPost("{id:int}/note")]
        [Authorize(Roles = "Admin,Manager,Accounting,Driver")]
        public async Task<IActionResult> AddNote(int id, [FromBody] AddShipmentNoteRequest request)
        {
            await _mediator.Send(new Akyildiz.Sevkiyat.Application.Shipments.Commands.AddShipmentNote.AddShipmentNoteCommand(id, request.Note));
            return NoContent();
        }
    }

    public record SendComparisonEmailRequest(List<string>? CcEmails);

    public record CancelShipmentRequest(string Reason, bool NotifyOutOfStock = false, List<string>? ExtraCc = null);

    public record DeliverStopRequest(
        string DeliveryRecipient,
        string? DeliveryNote,
        List<string>? PhotosBase64,
        double? Latitude,
        double? Longitude,
        List<DeliverStopLineInput>? Lines,
        List<DeliverStopExternalReturnInput>? ExternalReturns);

    public record AddShipmentNoteRequest(string Note);
}
