using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.DefinedReasons.Commands
{
    public record CreateDefinedReasonCommand(ReasonCategory Category, string Label, int SortOrder) : IRequest<int>;

    public class CreateDefinedReasonHandler : IRequestHandler<CreateDefinedReasonCommand, int>
    {
        private readonly IApplicationDbContext _context;
        public CreateDefinedReasonHandler(IApplicationDbContext context) => _context = context;

        public async Task<int> Handle(CreateDefinedReasonCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Label))
                throw new DomainException("Sebep etiketi boş olamaz.");

            var reason = new DefinedReason
            {
                Category = request.Category,
                Label = request.Label.Trim(),
                SortOrder = request.SortOrder,
                IsActive = true
            };
            _context.DefinedReasons.Add(reason);
            await _context.SaveChangesAsync(cancellationToken);
            return reason.Id;
        }
    }

    public record UpdateDefinedReasonCommand(int Id, ReasonCategory Category, string Label, int SortOrder, bool IsActive) : IRequest;

    public class UpdateDefinedReasonHandler : IRequestHandler<UpdateDefinedReasonCommand>
    {
        private readonly IApplicationDbContext _context;
        public UpdateDefinedReasonHandler(IApplicationDbContext context) => _context = context;

        public async Task Handle(UpdateDefinedReasonCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Label))
                throw new DomainException("Sebep etiketi boş olamaz.");

            var reason = await _context.DefinedReasons.FindAsync(new object[] { request.Id }, cancellationToken)
                ?? throw new NotFoundException("Sebep bulunamadı.");
            reason.Category = request.Category;
            reason.Label = request.Label.Trim();
            reason.SortOrder = request.SortOrder;
            reason.IsActive = request.IsActive;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public record DeleteDefinedReasonCommand(int Id) : IRequest;

    public class DeleteDefinedReasonHandler : IRequestHandler<DeleteDefinedReasonCommand>
    {
        private readonly IApplicationDbContext _context;
        public DeleteDefinedReasonHandler(IApplicationDbContext context) => _context = context;

        public async Task Handle(DeleteDefinedReasonCommand request, CancellationToken cancellationToken)
        {
            var reason = await _context.DefinedReasons.FindAsync(new object[] { request.Id }, cancellationToken)
                ?? throw new NotFoundException("Sebep bulunamadı.");
            _context.DefinedReasons.Remove(reason);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
