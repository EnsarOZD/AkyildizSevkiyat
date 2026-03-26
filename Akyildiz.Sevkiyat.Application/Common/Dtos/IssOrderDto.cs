using System;
using System.Collections.Generic;

namespace Akyildiz.Sevkiyat.Application.Common.Dtos
{
    public record IssOrderDto(
     int Id,
     string ExternalOrderNumber,
     DateTime OrderDate,
     DateTime DeliveryDate,
     int ProjectId,
     string ProjectCode,
     string ProjectName,
     string Region,
     IReadOnlyCollection<IssOrderLineDto> Lines
 );
}
