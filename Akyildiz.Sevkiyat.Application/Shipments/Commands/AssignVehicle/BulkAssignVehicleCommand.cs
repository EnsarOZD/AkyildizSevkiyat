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
        int DriverId,
        int VehicleId
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
            var errors = new List<string>();

            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.Id == request.DriverId, cancellationToken);
            if (driver == null || !driver.IsActive)
            {
                errors.Add("Seçilen şoför bulunamadı veya aktif değil.");
                return new BulkAssignVehicleResult(0, errors);
            }

            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.Id == request.VehicleId, cancellationToken);
            if (vehicle == null || !vehicle.IsActive)
            {
                errors.Add("Seçilen araç bulunamadı veya aktif değil.");
                return new BulkAssignVehicleResult(0, errors);
            }

            var shipments = await _context.Shipments
                .Include(s => s.Project)
                .Include(s => s.Lines)
                .Where(s => request.ShipmentIds.Contains(s.Id))
                .ToListAsync(cancellationToken);

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

                    shipment.SetDriverInfo(driver.FullName, vehicle.PlateNumber, driver.Id);
                    shipment.ChangeStatus(ShipmentStatus.AssignedToVehicle, _currentUserService.UserId);
                    
                    // V1 WORKAROUND: İlk versiyonda şöför paneli olmadığı için, araç ataması yapıldığında direkt 'Delivered' (Teslim) yapıyoruz.
                    shipment.ChangeStatus(ShipmentStatus.Delivered, _currentUserService.UserId, "V1 Otomatik Teslimat (Şöför paneli kullanımı olmadığı için.)");
                    
                    successCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"#{shipment.Id}: {ex.Message}");
                }
            }

            var foundIds = shipments.Select(s => s.Id).ToHashSet();
            foreach (var id in request.ShipmentIds.Where(id => !foundIds.Contains(id)))
                errors.Add($"#{id}: Sevkiyat bulunamadı.");

            if (successCount > 0)
                await _context.SaveChangesAsync(cancellationToken);

            return new BulkAssignVehicleResult(successCount, errors);
        }
    }
}
