using System;
using System.Collections.Generic;
using System.Text;

namespace Akyildiz.Sevkiyat.Application.Common.Dtos
{
    public record ShipmentLineDto(
        int Id,
        int OrderLineId,
        decimal DeliveredQty
    );
}
