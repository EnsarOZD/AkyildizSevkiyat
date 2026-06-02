using Akyildiz.Sevkiyat.Application.Warehouse.Commands.MarkWarehousePickup;
using Akyildiz.Sevkiyat.Application.Warehouse.Commands.AllocateMacroShortage;
using Akyildiz.Sevkiyat.Application.Warehouse.Commands.AllocateFoodShortage;
using Akyildiz.Sevkiyat.Application.Warehouse.Commands.ResetGidaHazirlikShipments;
using Akyildiz.Sevkiyat.Application.Warehouse.Commands.LockProjectPicking;
using Akyildiz.Sevkiyat.Application.Warehouse.Commands.LockZoneMacroPicking;
using Akyildiz.Sevkiyat.Application.Warehouse.Commands.DispatchZoneAsCargo;
using Akyildiz.Sevkiyat.Application.Warehouse.Commands.DispatchZoneAsFreight;
using Akyildiz.Sevkiyat.Application.Warehouse.Commands.AdminForceCloseZone;
using Akyildiz.Sevkiyat.Application.Warehouse.Commands.FetchZoneIrsaliye;
using Akyildiz.Sevkiyat.Application.Warehouse.Commands.MarkProjectMicroReady;
using Akyildiz.Sevkiyat.Application.Warehouse.Commands.StartZonePreparation;
using Akyildiz.Sevkiyat.Application.Warehouse.Commands.UpdateAggregatedLines;
using Akyildiz.Sevkiyat.Application.Warehouse.Commands.UpdateFoodPickLines;
using Akyildiz.Sevkiyat.Application.Warehouse.Commands.UpdateMicroLinesBulk;
using Akyildiz.Sevkiyat.Application.Warehouse.Commands.MarkFoodPreparationReady;
using Akyildiz.Sevkiyat.Application.Warehouse.Queries.GetFoodPickList;
using Akyildiz.Sevkiyat.Application.Warehouse.Queries.GetProjectMicroPickList;
using Akyildiz.Sevkiyat.Application.Warehouse.Queries.GetZonePreparationStatus;
using Akyildiz.Sevkiyat.Application.Warehouse.Queries.GetZoneOutOfCityPickList;
using Akyildiz.Sevkiyat.Application.Warehouse.Commands.MarkZoneOutOfCityReady;
using Akyildiz.Sevkiyat.Application.Warehouse.Commands.SaveOutOfCityPickingProgress;
using Akyildiz.Sevkiyat.Application.Warehouse.Commands.SyncWarehouseDashboard;
using Akyildiz.Sevkiyat.Application.Warehouse.Commands.QueryYkShipmentStatus;
using Akyildiz.Sevkiyat.Application.Warehouse.Commands.RetryYkShipmentRegistration;
using Akyildiz.Sevkiyat.Application.Warehouse.Commands.InitializeZonePreparation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class WarehouseController : ControllerBase
    {
        private readonly ISender _mediator;

        public WarehouseController(ISender mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<ZonePreparationDto>> GetDashboard([FromQuery] int zoneId, [FromQuery] DateTime date)
        {
            return await _mediator.Send(new GetZonePreparationStatusQuery(zoneId, date));
        }

        [HttpGet("dashboard-all")]
        public async Task<ActionResult<List<Akyildiz.Sevkiyat.Application.Warehouse.Queries.GetWarehouseDashboard.DashboardZoneDto>>> GetDashboardAll()
        {
            return await _mediator.Send(new Akyildiz.Sevkiyat.Application.Warehouse.Queries.GetWarehouseDashboard.GetWarehouseDashboardQuery());
        }

        [HttpPost("dashboard/sync")]
        public async Task<ActionResult<bool>> SyncDashboard([FromBody] SyncWarehouseDashboardCommand command)
        {
            return await _mediator.Send(command);
        }

        [HttpPost("zone-preparation/initialize")]
        public async Task<ActionResult<bool>> InitializeZonePreparation([FromBody] InitializeZonePreparationCommand command)
        {
            return await _mediator.Send(command);
        }

        [HttpGet("micro-pick-list")]
        public async Task<ActionResult<List<MicroPickItemDto>>> GetMicroPickList([FromQuery] int zpProjectId)
        {
            return await _mediator.Send(new GetProjectMicroPickListQuery(zpProjectId));
        }

        [HttpPost("mark-micro-ready")]
        public async Task<IActionResult> MarkMicroReady([FromBody] MarkProjectMicroReadyCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("update-micro-lines-bulk")]
        public async Task<ActionResult<bool>> UpdateMicroLinesBulk([FromBody] UpdateMicroLinesBulkCommand command)
        {
            return await _mediator.Send(command);
        }

        [HttpPost("update-aggregated-lines")]
        public async Task<ActionResult<bool>> UpdateAggregatedLines([FromBody] UpdateAggregatedLinesCommand command)
        {
            return await _mediator.Send(command);
        }

        [HttpPost("allocate-macro-shortage")]
        public async Task<ActionResult<bool>> AllocateMacroShortage([FromBody] AllocateMacroShortageCommand command)
        {
            return await _mediator.Send(command);
        }

        [HttpPost("allocate-food-shortage")]
        public async Task<ActionResult<bool>> AllocateFoodShortage([FromBody] AllocateFoodShortageCommand command)
        {
            return await _mediator.Send(command);
        }

        [HttpGet("macro-pick-list")]
        public async Task<ActionResult<List<Akyildiz.Sevkiyat.Application.Warehouse.Queries.GetZoneMacroPickList.MacroPickItemDto>>> GetMacroPickList([FromQuery] int zonePreparationId)
        {
            return await _mediator.Send(new Akyildiz.Sevkiyat.Application.Warehouse.Queries.GetZoneMacroPickList.GetZoneMacroPickListQuery(zonePreparationId));
        }

        [HttpPost("mark-macro-ready")]
        public async Task<IActionResult> MarkMacroReady([FromBody] Akyildiz.Sevkiyat.Application.Warehouse.Commands.MarkZoneMacroReady.MarkZoneMacroReadyCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("set-driver-info")]
        public async Task<ActionResult<Akyildiz.Sevkiyat.Application.Warehouse.Commands.SetZoneDriverInfo.SetZoneDriverInfoResult>> SetDriverInfo(
            [FromBody] Akyildiz.Sevkiyat.Application.Warehouse.Commands.SetZoneDriverInfo.SetZoneDriverInfoCommand command)
        {
            return await _mediator.Send(command);
        }

        [HttpPost("start-zone-preparation")]
        public async Task<ActionResult<bool>> StartZonePreparation([FromBody] StartZonePreparationCommand command)
        {
            return await _mediator.Send(command);
        }

        [HttpPost("fetch-irsaliye")]
        public async Task<ActionResult<FetchZoneIrsaliyeResult>> FetchIrsaliye([FromBody] FetchZoneIrsaliyeCommand command)
        {
            return await _mediator.Send(command);
        }

        [HttpGet("pre-dispatch-summary")]
        public async Task<ActionResult<Akyildiz.Sevkiyat.Application.Warehouse.Services.PreDispatchSummaryDto>> GetPreDispatchSummary([FromQuery] int zpId)
        {
            return await _mediator.Send(new Akyildiz.Sevkiyat.Application.Warehouse.Queries.GetPreDispatchSummary.GetPreDispatchSummaryQuery(zpId));
        }

        [HttpPost("confirm-loading")]
        public async Task<ActionResult<bool>> ConfirmLoading([FromBody] Akyildiz.Sevkiyat.Application.Warehouse.Commands.ConfirmZoneLoading.ConfirmZoneLoadingCommand command)
        {
            return await _mediator.Send(command);
        }

        [HttpPost("dispatch-as-cargo")]
        public async Task<IActionResult> DispatchAsCargo([FromBody] DispatchZoneAsCargoCommand command)
        {
            await _mediator.Send(command);
            return Ok();
        }

        [HttpPost("dispatch-as-freight")]
        public async Task<IActionResult> DispatchAsFreight([FromBody] DispatchZoneAsFreightCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpGet("out-of-city-pick-list")]
        public async Task<IActionResult> GetOutOfCityPickList([FromQuery] int zonePreparationId, [FromQuery] int? projectId = null)
        {
            var result = await _mediator.Send(new GetZoneOutOfCityPickListQuery(zonePreparationId, projectId));
            return Ok(result);
        }

        [HttpPost("mark-out-of-city-ready")]
        public async Task<IActionResult> MarkOutOfCityReady([FromBody] MarkZoneOutOfCityReadyCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("save-out-of-city-progress")]
        public async Task<IActionResult> SaveOutOfCityProgress([FromBody] SaveOutOfCityPickingProgressCommand command)
        {
            await _mediator.Send(command);
            return Ok();
        }

        [HttpPost("admin-force-close-zone")]
        public async Task<IActionResult> AdminForceCloseZone([FromBody] AdminForceCloseZoneCommand command)
        {
            await _mediator.Send(command);
            return Ok();
        }

        [HttpPost("lock-project-picking")]
        public async Task<IActionResult> LockProjectPicking([FromBody] LockProjectPickingCommand command)
        {
            await _mediator.Send(command);
            return Ok();
        }

        [HttpPost("lock-zone-macro-picking")]
        public async Task<IActionResult> LockZoneMacroPicking([FromBody] LockZoneMacroPickingCommand command)
        {
            await _mediator.Send(command);
            return Ok();
        }

        [HttpGet("zone-verification")]
        public async Task<IActionResult> GetZoneVerification([FromQuery] int zpId)
        {
            var result = await _mediator.Send(new Akyildiz.Sevkiyat.Application.Warehouse.Queries.GetZoneVerification.GetZoneVerificationQuery(zpId));
            return Ok(result);
        }

        [HttpGet("shipments/{id:int}/yk-status")]
        public async Task<IActionResult> QueryYkStatus(int id)
        {
            var result = await _mediator.Send(new QueryYkShipmentStatusCommand { ShipmentId = id });
            return Ok(result);
        }

        [HttpPost("shipments/{id:int}/yk-register")]
        public async Task<IActionResult> RetryYkRegister(int id, [FromBody] RetryYkRegisterRequest body)
        {
            var result = await _mediator.Send(new RetryYkShipmentRegistrationCommand
            {
                ShipmentId = id,
                PieceCount = body.PieceCount,
                Desi       = body.Desi,
            });
            return Ok(result);
        }

        public record RetryYkRegisterRequest(int PieceCount = 1, decimal Desi = 1m);

        // ── Gıda Hazırlık ──────────────────────────────────────────────────────

        [HttpGet("food-pick-list")]
        public async Task<ActionResult<List<FoodPickItemDto>>> GetFoodPickList(
            [FromQuery] int zoneId,
            [FromQuery] DateTime deliveryDate)
        {
            return await _mediator.Send(new GetFoodPickListQuery(zoneId, deliveryDate));
        }

        [HttpPost("update-food-pick-lines")]
        public async Task<ActionResult<bool>> UpdateFoodPickLines([FromBody] UpdateFoodPickLinesCommand command)
        {
            return await _mediator.Send(command);
        }

        [HttpPost("mark-food-preparation-ready")]
        public async Task<ActionResult<MarkFoodPreparationReadyResult>> MarkFoodPreparationReady(
            [FromBody] MarkFoodPreparationReadyCommand command)
        {
            return await _mediator.Send(command);
        }

        [HttpPost("zone-preparations/{id:int}/reset-shipment-status")]
        public async Task<ActionResult<ResetGidaHazirlikResult>> ResetGidaHazirlikShipments(int id)
        {
            return await _mediator.Send(new ResetGidaHazirlikShipmentsCommand(id));
        }

        [HttpPost("shipments/{id:int}/warehouse-pickup")]
        public async Task<IActionResult> WarehousePickup(int id, [FromBody] WarehousePickupRequest body)
        {
            await _mediator.Send(new MarkWarehousePickupCommand(id, body.RecipientName, body.Note));
            return Ok();
        }
    }

    public record WarehousePickupRequest(string RecipientName, string? Note = null);
}
