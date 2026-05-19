using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.PurchaseOrders.Commands.MarkEmailSent
{
    public record MarkEmailSentCommand : IRequest
    {
        public Guid Id { get; init; }
        public string? SentTo { get; init; }
    }

    public class MarkEmailSentCommandHandler : IRequestHandler<MarkEmailSentCommand>
    {
        private readonly IApplicationDbContext _context;

        public MarkEmailSentCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(MarkEmailSentCommand request, CancellationToken cancellationToken)
        {
            var order = await _context.PurchaseOrders
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
                ?? throw new NotFoundException("Satınalma siparişi bulunamadı.");

            order.MarkEmailSent(request.SentTo?.Trim());
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
