using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Carriers.Commands.DeleteCarrier
{
    public record DeleteCarrierCommand(int Id) : IRequest;

    public class DeleteCarrierCommandHandler : IRequestHandler<DeleteCarrierCommand>
    {
        private readonly IApplicationDbContext _context;

        public DeleteCarrierCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(DeleteCarrierCommand request, CancellationToken cancellationToken)
        {
            var carrier = await _context.Carriers
                .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
                ?? throw new NotFoundException("Carrier", request.Id);

            // Plakalar FK cascade ile birlikte silinir
            _context.Carriers.Remove(carrier);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
