using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.Settings;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.AssignVehicle
{
    public record BulkAssignVehicleCommand(
        List<int> ShipmentIds,
        string DriverName,
        string PlateNumber
    ) : IRequest<BulkAssignVehicleResult>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Dispatcher" };
    }

    public record BulkAssignVehicleResult(int SuccessCount, List<string> Errors);

    public class BulkAssignVehicleCommandHandler : IRequestHandler<BulkAssignVehicleCommand, BulkAssignVehicleResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ShipmentSettings _settings;

        public BulkAssignVehicleCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IOptions<ShipmentSettings> settings)
        {
            _context = context;
            _currentUserService = currentUserService;
            _settings = settings.Value;
        }

        public async Task<BulkAssignVehicleResult> Handle(BulkAssignVehicleCommand request, CancellationToken cancellationToken)
        {
            var shipments = await _context.Shipments
                .Include(s => s.Project)
                .Include(s => s.Lines)
                .Where(s => request.ShipmentIds.Contains(s.Id))
                .ToListAsync(cancellationToken);

            var errors = new List<string>();
            int successCount = 0;

            foreach (var shipment in shipments)
            {
                try
                {
                    if (shipment.Status != ShipmentStatus.ReadyForDispatch)
                    {
                        errors.Add($"#{shipment.Id}: Sevkiyat 'Sevke Hazır' durumunda değil (mevcut: {shipment.Status}).");
                        continue;
                    }

                    if (!shipment.Project.ZoneId.HasValue)
                    {
                        errors.Add($"#{shipment.Id}: Projeye bölge atanmamış.");
                        continue;
                    }

                    if (_settings.RequireIrsaliyeNoOnDispatch && string.IsNullOrWhiteSpace(shipment.IrsaliyeNo))
                    {
                        errors.Add($"#{shipment.Id}: İrsaliye numarası eksik.");
                        continue;
                    }

                    shipment.SetDriverInfo(request.DriverName, request.PlateNumber);
                    shipment.ChangeStatus(ShipmentStatus.AssignedToVehicle, _currentUserService.UserId);
                    successCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"#{shipment.Id}: {ex.Message}");
                }
            }

            // Requested IDs not found in DB
            var foundIds = shipments.Select(s => s.Id).ToHashSet();
            foreach (var id in request.ShipmentIds.Where(id => !foundIds.Contains(id)))
                errors.Add($"#{id}: Sevkiyat bulunamadı.");

            if (successCount > 0)
                await _context.SaveChangesAsync(cancellationToken);

            return new BulkAssignVehicleResult(successCount, errors);
        }
    }
}
