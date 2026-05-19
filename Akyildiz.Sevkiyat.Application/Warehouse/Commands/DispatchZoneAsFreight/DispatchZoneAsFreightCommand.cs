using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.DispatchZoneAsFreight
{
    public record DispatchZoneAsFreightCommand : IRequest<Unit>, IRequireRoles
    {
        public int ZonePreparationId { get; init; }
        public string CarrierName { get; init; } = string.Empty;
        public string? CarrierPlate { get; init; }
        public string? CarrierPhone { get; init; }

        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting", "Warehouse" };
    }

    public class DispatchZoneAsFreightCommandValidator : AbstractValidator<DispatchZoneAsFreightCommand>
    {
        public DispatchZoneAsFreightCommandValidator()
        {
            RuleFor(x => x.ZonePreparationId).GreaterThan(0);
            RuleFor(x => x.CarrierName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.CarrierPlate).MaximumLength(20).When(x => x.CarrierPlate != null);
            RuleFor(x => x.CarrierPhone).MaximumLength(30).When(x => x.CarrierPhone != null);
        }
    }

    public class DispatchZoneAsFreightCommandHandler : IRequestHandler<DispatchZoneAsFreightCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public DispatchZoneAsFreightCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Unit> Handle(DispatchZoneAsFreightCommand request, CancellationToken cancellationToken)
        {
            var zp = await _context.ZonePreparations
                .FirstOrDefaultAsync(z => z.Id == request.ZonePreparationId, cancellationToken)
                ?? throw new NotFoundException("ZonePreparation", request.ZonePreparationId);

            if (zp.Status != ZonePreparationStatus.ReadyForDriverInfo)
                throw new DomainException("Nakliye gönderimi için hazırlık 'Araç/Kargo Ataması' aşamasında olmalıdır.");

            if (!zp.IsFrozen)
                throw new DomainException("Hazırlık başlatılmadan nakliye ataması yapılamaz.");

            if (!zp.IrsaliyeFetched)
                throw new DomainException("Nakliye göndermeden önce Netsisten irsaliye numaraları çekilmelidir.");

            var shipments = await _context.WarehouseShipments
                .Where(s =>
                    s.ZonePreparationId == zp.Id &&
                    s.Status != ShipmentStatus.Cancelled &&
                    s.Status != ShipmentStatus.Passive &&
                    s.Status < ShipmentStatus.Dispatched)
                .ToListAsync(cancellationToken);

            foreach (var s in shipments)
            {
                s.ChangeStatus(ShipmentStatus.Dispatched, _currentUserService.UserId);
                s.SetFreightDispatch(request.CarrierName, request.CarrierPlate, request.CarrierPhone);
            }

            zp.Status = ZonePreparationStatus.Dispatched;

            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
