using System;
using System.Collections.Generic;
using System.Text;

namespace Akyildiz.Sevkiyat.Application.Common.Dtos
{
    public record ShipmentDto(
        int Id,
        DateTime ShipmentDate,
        int? OrderId,
        List<ShipmentLineDto> Lines
    );
}