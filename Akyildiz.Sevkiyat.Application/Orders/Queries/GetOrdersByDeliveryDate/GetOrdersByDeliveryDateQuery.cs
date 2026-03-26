using System;
using System.Collections.Generic;
using Akyildiz.Sevkiyat.Application.Common.Dtos;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.Orders.Queries.GetOrdersByDeliveryDate
{
    public record GetOrdersByDeliveryDateQuery(
        DateTime DeliveryDate
    ) : IRequest<List<IssOrderDto>>;
}
