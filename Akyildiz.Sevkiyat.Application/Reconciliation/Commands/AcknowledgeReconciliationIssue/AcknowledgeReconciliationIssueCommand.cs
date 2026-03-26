using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Reconciliation.Commands.AcknowledgeReconciliationIssue
{
    /// <summary>
    /// Tutarsızlık kaydını "Acknowledged" olarak işaretler.
    /// Admin, sorunu gördüğünü ve sebebini not ettiğini onaylar.
    /// </summary>
    public record AcknowledgeReconciliationIssueCommand(
        int IssueId,
        string Note
    ) : IRequest<bool>;

    public class AcknowledgeReconciliationIssueCommandHandler
        : IRequestHandler<AcknowledgeReconciliationIssueCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public AcknowledgeReconciliationIssueCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<bool> Handle(AcknowledgeReconciliationIssueCommand request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Note))
                throw new DomainException("Acknowledge işlemi için açıklama zorunludur.");

            var issue = await _context.ReconciliationIssues
                .FirstOrDefaultAsync(i => i.Id == request.IssueId, ct)
                ?? throw new NotFoundException("ReconciliationIssue", request.IssueId);

            if (issue.Status == ReconciliationStatus.AutoResolved)
                throw new DomainException("AutoResolved sorunlar acknowledge edilemez — sorun zaten kendiliğinden kapandı.");

            issue.Status                = ReconciliationStatus.Acknowledged;
            issue.AcknowledgedAt        = DateTime.UtcNow;
            issue.AcknowledgedByUserId  = _currentUserService.UserId;
            issue.AcknowledgementNote   = request.Note;

            await _context.SaveChangesAsync(ct);
            return true;
        }
    }
}
