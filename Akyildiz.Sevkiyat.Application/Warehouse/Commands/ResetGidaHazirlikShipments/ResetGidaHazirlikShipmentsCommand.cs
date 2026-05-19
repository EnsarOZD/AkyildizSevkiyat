using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.ResetGidaHazirlikShipments
{
    /// <summary>
    /// Gıda hazırlık aşamasında olan bir zone preparation'ın sevkiyatlarını
    /// ReadyForDispatch'ten Picking'e geri çeker.
    /// Hatalı geçişleri (eski bug) düzeltmek için admin'e açık bir onarım komutudur.
    /// </summary>
    public record ResetGidaHazirlikShipmentsCommand(int ZonePreparationId)
        : IRequest<ResetGidaHazirlikResult>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin" };
    }

    public class ResetGidaHazirlikResult
    {
        public int ResetCount { get; set; }
    }

    public class ResetGidaHazirlikShipmentsCommandHandler
        : IRequestHandler<ResetGidaHazirlikShipmentsCommand, ResetGidaHazirlikResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public ResetGidaHazirlikShipmentsCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<ResetGidaHazirlikResult> Handle(
            ResetGidaHazirlikShipmentsCommand request,
            CancellationToken cancellationToken)
        {
            var zp = await _context.ZonePreparations
                .FirstOrDefaultAsync(z => z.Id == request.ZonePreparationId, cancellationToken)
                ?? throw new NotFoundException("ZonePreparation", request.ZonePreparationId);

            if (zp.Status != ZonePreparationStatus.GidaHazirlik)
                throw new DomainException("Bu işlem yalnızca Gıda Hazırlık aşamasındaki hazırlıklar için geçerlidir.");

            var shipments = await _context.WarehouseShipments
                .Where(s =>
                    s.ZonePreparationId == zp.Id &&
                    s.Status == ShipmentStatus.ReadyForDispatch)
                .ToListAsync(cancellationToken);

            foreach (var s in shipments)
                s.ChangeStatus(ShipmentStatus.Picking, _currentUser.UserId);

            await _context.SaveChangesAsync(cancellationToken);

            return new ResetGidaHazirlikResult { ResetCount = shipments.Count };
        }
    }
}
