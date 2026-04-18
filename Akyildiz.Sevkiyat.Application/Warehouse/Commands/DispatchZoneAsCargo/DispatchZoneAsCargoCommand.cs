using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.DispatchZoneAsCargo
{
    public record DispatchZoneAsCargoCommand : IRequest<Unit>, IRequireRoles
    {
        public int ZonePreparationId { get; init; }
        public CargoProvider CargoProvider { get; init; }
        public string? CargoTrackingNumber { get; init; }

        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Warehouse" };
    }

    public class DispatchZoneAsCargoCommandValidator : AbstractValidator<DispatchZoneAsCargoCommand>
    {
        public DispatchZoneAsCargoCommandValidator()
        {
            RuleFor(x => x.ZonePreparationId).GreaterThan(0);
            RuleFor(x => x.CargoProvider).IsInEnum();
        }
    }

    public class DispatchZoneAsCargoCommandHandler : IRequestHandler<DispatchZoneAsCargoCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public DispatchZoneAsCargoCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Unit> Handle(DispatchZoneAsCargoCommand request, CancellationToken cancellationToken)
        {
            var zp = await _context.ZonePreparations
                .FirstOrDefaultAsync(z => z.Id == request.ZonePreparationId, cancellationToken)
                ?? throw new NotFoundException("ZonePreparation", request.ZonePreparationId);

            if (zp.Status != ZonePreparationStatus.ReadyForDriverInfo)
                throw new DomainException("Kargo gönderimi için hazırlık 'Araç/Kargo Ataması' aşamasında olmalıdır.");

            if (!zp.IsFrozen)
                throw new DomainException("Hazırlık başlatılmadan kargo ataması yapılamaz.");

            if (!zp.IrsaliyeFetched)
                throw new DomainException("Kargo göndermeden önce Netsisten irsaliye numaraları çekilmelidir.");

            var shipments = await _context.Shipments
                .Where(s =>
                    s.ZonePreparationId == zp.Id &&
                    s.Status != ShipmentStatus.Cancelled &&
                    s.Status != ShipmentStatus.Passive &&
                    s.Status < ShipmentStatus.Dispatched)
                .ToListAsync(cancellationToken);

            foreach (var s in shipments)
            {
                s.ChangeStatus(ShipmentStatus.Dispatched, _currentUserService.UserId);
                s.SetCargoDispatch(request.CargoProvider, request.CargoTrackingNumber);
            }

            zp.Status = ZonePreparationStatus.Dispatched;

            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
