using Akyildiz.Sevkiyat.Application.Shipments.Commands.UpdateShipmentQuantities;
using System.Collections.Generic;

namespace Akyildiz.Sevkiyat.WebApi.Controllers.Models
{
    public class UpdateShipmentQuantitiesRequest
    {
        public List<ShipmentLineUpdateDto> Lines { get; set; } = new();
    }
}
