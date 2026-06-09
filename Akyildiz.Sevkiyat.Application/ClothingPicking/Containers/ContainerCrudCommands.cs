using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.ClothingPicking.Containers
{
    public record ContainerDto(int Id, string Code, int Type, bool IsActive);

    // ── List ──────────────────────────────────────────────────────────────
    public record GetContainersQuery(bool ActiveOnly = false) : IRequest<List<ContainerDto>>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager", "Accounting", "Warehouse" };
    }

    public class GetContainersQueryHandler : IRequestHandler<GetContainersQuery, List<ContainerDto>>
    {
        private readonly IApplicationDbContext _context;
        public GetContainersQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<List<ContainerDto>> Handle(GetContainersQuery request, CancellationToken ct)
        {
            var q = _context.Containers.AsQueryable();
            if (request.ActiveOnly) q = q.Where(c => c.IsActive);
            return await q.OrderBy(c => c.Code)
                .Select(c => new ContainerDto(c.Id, c.Code, (int)c.Type, c.IsActive))
                .ToListAsync(ct);
        }
    }

    // ── Save (create/update) — Code unique ──────────────────────────────────
    public record SaveContainerCommand(int? Id, string Code, PickingContainerType Type, bool IsActive)
        : IRequest<int>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager" };
    }

    public class SaveContainerCommandValidator : AbstractValidator<SaveContainerCommand>
    {
        public SaveContainerCommandValidator()
        {
            RuleFor(x => x.Code).NotEmpty().WithMessage("Araba kodu zorunludur.").MaximumLength(50);
        }
    }

    public class SaveContainerCommandHandler : IRequestHandler<SaveContainerCommand, int>
    {
        private readonly IApplicationDbContext _context;
        public SaveContainerCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<int> Handle(SaveContainerCommand request, CancellationToken ct)
        {
            var code = request.Code.Trim();

            // Code benzersizliği (DB unique index var; temiz hata için ön kontrol)
            var dup = await _context.Containers
                .AnyAsync(c => c.Code == code && (request.Id == null || c.Id != request.Id), ct);
            if (dup) throw new ConflictException($"'{code}' kodlu araba zaten mevcut.");

            Container entity;
            if (request.Id is > 0)
            {
                entity = await _context.Containers.FirstOrDefaultAsync(c => c.Id == request.Id, ct)
                    ?? throw new NotFoundException("Container", request.Id.Value);
            }
            else
            {
                entity = new Container();
                _context.Containers.Add(entity);
            }

            entity.Code = code;
            entity.Type = request.Type;
            entity.IsActive = request.IsActive;

            await _context.SaveChangesAsync(ct);
            return entity.Id;
        }
    }

    // ── Soft delete (hard delete YOK) ───────────────────────────────────────
    public record DeactivateContainerCommand(int Id) : IRequest<Unit>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager" };
    }

    public class DeactivateContainerCommandHandler : IRequestHandler<DeactivateContainerCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        public DeactivateContainerCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Unit> Handle(DeactivateContainerCommand request, CancellationToken ct)
        {
            var entity = await _context.Containers.FirstOrDefaultAsync(c => c.Id == request.Id, ct)
                ?? throw new NotFoundException("Container", request.Id);
            entity.IsActive = false;
            await _context.SaveChangesAsync(ct);
            return Unit.Value;
        }
    }
}
