using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.Projects.Commands.UpdateProjectDeliveryWindow
{
    public record UpdateProjectDeliveryWindowCommand(
        int ProjectId,
        TimeOnly? DeliveryWindowStart,
        TimeOnly? DeliveryWindowEnd
    ) : IRequest;

    public class UpdateProjectDeliveryWindowCommandHandler : IRequestHandler<UpdateProjectDeliveryWindowCommand>
    {
        private readonly IApplicationDbContext _context;

        public UpdateProjectDeliveryWindowCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(UpdateProjectDeliveryWindowCommand request, CancellationToken cancellationToken)
        {
            var project = await _context.Projects.FindAsync(new object[] { request.ProjectId }, cancellationToken)
                ?? throw new NotFoundException("Project", request.ProjectId);

            if (request.DeliveryWindowStart.HasValue && request.DeliveryWindowEnd.HasValue
                && request.DeliveryWindowStart.Value >= request.DeliveryWindowEnd.Value)
                throw new DomainException("Başlangıç saati bitiş saatinden önce olmalıdır.");

            project.DeliveryWindowStart = request.DeliveryWindowStart;
            project.DeliveryWindowEnd   = request.DeliveryWindowEnd;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
