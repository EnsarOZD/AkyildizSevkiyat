using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.UpdateIrsaliyeNo
{
    public record UpdateIrsaliyeNoCommand(
        int ShipmentId,
        string IrsaliyeNo,
        DateOnly IrsaliyeDate
    ) : IRequest, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Dispatcher" };
    }

    public class UpdateIrsaliyeNoCommandValidator : AbstractValidator<UpdateIrsaliyeNoCommand>
    {
        public UpdateIrsaliyeNoCommandValidator()
        {
            RuleFor(x => x.IrsaliyeNo).NotEmpty().MaximumLength(50);
        }
    }

    public class UpdateIrsaliyeNoCommandHandler : IRequestHandler<UpdateIrsaliyeNoCommand>
    {
        private readonly IApplicationDbContext _context;

        public UpdateIrsaliyeNoCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(UpdateIrsaliyeNoCommand request, CancellationToken cancellationToken)
        {
            var shipment = await _context.Shipments
                .FirstOrDefaultAsync(s => s.Id == request.ShipmentId, cancellationToken)
                ?? throw new NotFoundException("Sevkiyat", request.ShipmentId);

            shipment.SetIrsaliyeInfo(request.IrsaliyeNo.Trim(), request.IrsaliyeDate);
            if (!shipment.NetsisTransferredAt.HasValue)
                shipment.MarkNetsisTransferred(DateTime.UtcNow);

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
