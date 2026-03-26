using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.Projects.Commands.UpdateProjectDeliveryOrder
{
    public record UpdateProjectDeliveryOrderCommand(int ProjectId, int? DeliveryOrder) : IRequest;

    public class UpdateProjectDeliveryOrderCommandHandler : IRequestHandler<UpdateProjectDeliveryOrderCommand>
    {
        private readonly IApplicationDbContext _context;

        public UpdateProjectDeliveryOrderCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(UpdateProjectDeliveryOrderCommand request, CancellationToken cancellationToken)
        {
            var project = await _context.Projects.FindAsync(new object[] { request.ProjectId }, cancellationToken)
                ?? throw new NotFoundException("Project", request.ProjectId);

            project.DeliveryOrder = request.DeliveryOrder;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
