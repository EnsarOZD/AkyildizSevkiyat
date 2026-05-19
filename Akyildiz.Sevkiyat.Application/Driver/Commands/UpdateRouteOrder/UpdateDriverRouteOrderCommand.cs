using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Driver.Commands.UpdateRouteOrder
{
    public record RouteOrderItem(int ProjectId, int RouteOrder);

    public record UpdateDriverRouteOrderCommand : IRequest<Unit>, IRequireRoles
    {
        public int ZonePreparationId { get; init; }
        public List<RouteOrderItem> Items { get; init; } = new();

        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting", "Driver" };
    }

    public class UpdateDriverRouteOrderCommandHandler
        : IRequestHandler<UpdateDriverRouteOrderCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public UpdateDriverRouteOrderCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Unit> Handle(
            UpdateDriverRouteOrderCommand request,
            CancellationToken cancellationToken)
        {
            if (!request.Items.Any())
                return Unit.Value;

            // Driver role can only reorder zones they are assigned to
            if (_currentUserService.Role == Domain.Enums.UserRole.Driver)
            {
                var driver = await _context.Drivers
                    .FirstOrDefaultAsync(d => d.UserId == _currentUserService.UserId, cancellationToken)
                    ?? throw new ForbiddenException("Kullanıcıya tanımlı bir şoför kaydı bulunamadı.");

                var isAssigned = await _context.ZonePreparationDrivers
                    .AnyAsync(zpd =>
                        zpd.ZonePreparationId == request.ZonePreparationId &&
                        zpd.DriverId == driver.Id, cancellationToken);

                if (!isAssigned)
                    throw new ForbiddenException("Bu bölgeye atanmadınız.");
            }

            var zpProjects = await _context.ZonePreparationProjects
                .Where(zpp => zpp.ZonePreparationId == request.ZonePreparationId)
                .ToListAsync(cancellationToken);

            if (!zpProjects.Any())
                throw new NotFoundException("ZonePreparation", request.ZonePreparationId);

            var lookup = request.Items.ToDictionary(i => i.ProjectId, i => i.RouteOrder);

            foreach (var zpp in zpProjects)
            {
                if (lookup.TryGetValue(zpp.ProjectId, out var order))
                    zpp.RouteOrder = order;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
