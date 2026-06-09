using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.ClothingPicking.PickingGroups
{
    public record PickingGroupDto(int Id, string Name, int SortOrder, bool IsActive);

    // ── List ──────────────────────────────────────────────────────────────
    public record GetPickingGroupsQuery(bool ActiveOnly = false) : IRequest<List<PickingGroupDto>>;

    public class GetPickingGroupsQueryHandler : IRequestHandler<GetPickingGroupsQuery, List<PickingGroupDto>>
    {
        private readonly IApplicationDbContext _context;
        public GetPickingGroupsQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<List<PickingGroupDto>> Handle(GetPickingGroupsQuery request, CancellationToken ct)
        {
            var q = _context.PickingGroups.AsQueryable();
            if (request.ActiveOnly) q = q.Where(g => g.IsActive);
            return await q.OrderBy(g => g.SortOrder).ThenBy(g => g.Name)
                .Select(g => new PickingGroupDto(g.Id, g.Name, g.SortOrder, g.IsActive))
                .ToListAsync(ct);
        }
    }

    // ── Save (create/update) ───────────────────────────────────────────────
    public record SavePickingGroupCommand(int? Id, string Name, int SortOrder, bool IsActive)
        : IRequest<int>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager" };
    }

    public class SavePickingGroupCommandValidator : AbstractValidator<SavePickingGroupCommand>
    {
        public SavePickingGroupCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Grup adı zorunludur.").MaximumLength(100);
        }
    }

    public class SavePickingGroupCommandHandler : IRequestHandler<SavePickingGroupCommand, int>
    {
        private readonly IApplicationDbContext _context;
        public SavePickingGroupCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<int> Handle(SavePickingGroupCommand request, CancellationToken ct)
        {
            PickingGroup entity;
            if (request.Id is > 0)
            {
                entity = await _context.PickingGroups.FirstOrDefaultAsync(g => g.Id == request.Id, ct)
                    ?? throw new NotFoundException("PickingGroup", request.Id.Value);
            }
            else
            {
                entity = new PickingGroup();
                _context.PickingGroups.Add(entity);
            }

            entity.Name = request.Name.Trim();
            entity.SortOrder = request.SortOrder;
            entity.IsActive = request.IsActive;

            await _context.SaveChangesAsync(ct);
            return entity.Id;
        }
    }

    // ── Soft delete (HARD DELETE YOK — öksüz PickingGroupId koruması) ───────
    public record DeactivatePickingGroupCommand(int Id) : IRequest<Unit>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager" };
    }

    public class DeactivatePickingGroupCommandHandler : IRequestHandler<DeactivatePickingGroupCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        public DeactivatePickingGroupCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Unit> Handle(DeactivatePickingGroupCommand request, CancellationToken ct)
        {
            var entity = await _context.PickingGroups.FirstOrDefaultAsync(g => g.Id == request.Id, ct)
                ?? throw new NotFoundException("PickingGroup", request.Id);
            entity.IsActive = false;   // sadece pasifleştir
            await _context.SaveChangesAsync(ct);
            return Unit.Value;
        }
    }
}
