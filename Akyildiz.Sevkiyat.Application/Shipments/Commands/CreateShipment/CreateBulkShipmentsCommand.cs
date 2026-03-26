using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.CreateShipment
{
    public class CreateBulkShipmentsCommand : IRequest<int>, IRequireRoles
    {
        public List<int> IssOrderIds { get; set; } = new();

        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting" };
    }

    public class CreateBulkShipmentsCommandHandler : IRequestHandler<CreateBulkShipmentsCommand, int>
    {
        private readonly ISender _mediator;

        public CreateBulkShipmentsCommandHandler(ISender mediator)
        {
            _mediator = mediator;
        }

        public async Task<int> Handle(CreateBulkShipmentsCommand request, CancellationToken cancellationToken)
        {
            int successCount = 0;
            foreach (var orderId in request.IssOrderIds)
            {
                try
                {
                    // We call the single creation command for each.
                    // This ensures all validation logic (duplicate checks, etc.) is preserved.
                    await _mediator.Send(new CreateShipmentCommand { IssOrderId = orderId }, cancellationToken);
                    successCount++;
                }
                catch
                {
                    // If one fails, we continue with others? 
                    // User requirement: "Create shipments for selected". 
                    // Silently fail or log? For now, we continue and maybe return count.
                    // Ideally we should return a report, but for now just count of successes.
                }
            }
            return successCount;
        }
    }
}
