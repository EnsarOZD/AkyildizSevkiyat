using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.ClothingPicking.Shortages
{
    public record CancelShortageCommand(int Id, string Reason) : IRequest<Unit>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager", "Accounting", "Warehouse" };
    }

    public class CancelShortageCommandValidator : AbstractValidator<CancelShortageCommand>
    {
        public CancelShortageCommandValidator()
        {
            RuleFor(x => x.Reason).NotEmpty().WithMessage("İptal sebebi zorunludur.").MaximumLength(500);
        }
    }

    public class CancelShortageCommandHandler : IRequestHandler<CancelShortageCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        public CancelShortageCommandHandler(IApplicationDbContext c, ICurrentUserService u) { _context = c; _currentUser = u; }

        public async Task<Unit> Handle(CancelShortageCommand request, CancellationToken ct)
        {
            var r = await _context.ShortageRecords.FirstOrDefaultAsync(x => x.Id == request.Id, ct)
                ?? throw new NotFoundException("ShortageRecord", request.Id);

            if (r.Status is ShortageStatus.Shipped or ShortageStatus.Cancelled)
                throw new DomainException($"Bu eksik kaydı '{r.Status}' durumunda; iptal edilemez.");

            r.Status = ShortageStatus.Cancelled;
            r.Note = string.IsNullOrWhiteSpace(r.Note) ? $"İptal: {request.Reason.Trim()}" : $"{r.Note} | İptal: {request.Reason.Trim()}";
            r.ResolvedAt = DateTime.UtcNow;
            r.ResolvedByUserId = _currentUser.UserId;

            await _context.SaveChangesAsync(ct);
            return Unit.Value;
        }
    }
}
