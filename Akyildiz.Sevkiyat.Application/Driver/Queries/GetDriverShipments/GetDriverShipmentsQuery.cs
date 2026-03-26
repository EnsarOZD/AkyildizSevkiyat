using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Application.Driver.Queries.GetDriverShipments
{
    /// <summary>
    /// Şoför paneli için: AssignedToVehicle + bugün teslim edilen sevkiyatlar.
    /// </summary>
    public record GetDriverShipmentsQuery : IRequest<List<DriverShipmentDto>>;

    public class DriverShipmentDto
    {
        public int Id { get; set; }
        public string? TalepNo { get; set; }
        public DateTime DeliveryDate { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string? ProjectAddress { get; set; }
        public string? TeslimAlacakKisiler { get; set; }
        public string? TeslimAlacakTelefon { get; set; }
        public string? DriverName { get; set; }
        public string? PlateNumber { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? DeliveredAt { get; set; }
        public string? DeliveryPhotoBase64 { get; set; }
        public int LineCount { get; set; }
    }

    public class GetDriverShipmentsQueryHandler : IRequestHandler<GetDriverShipmentsQuery, List<DriverShipmentDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetDriverShipmentsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<List<DriverShipmentDto>> Handle(GetDriverShipmentsQuery request, CancellationToken cancellationToken)
        {
            var today = DateTime.UtcNow.Date;

            int? driverId = null;
            if (_currentUserService.Role == UserRole.Driver)
            {
                var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == _currentUserService.UserId, cancellationToken)
                    ?? throw new ForbiddenException("Kullanıcıya tanımlı bir şoför kaydı bulunamadı.");
                driverId = driver.Id;
            }

            var query = _context.Shipments
                .Include(s => s.Project)
                .Include(s => s.IssOrder)
                .Include(s => s.Lines)
                .Where(s =>
                    s.Status == ShipmentStatus.AssignedToVehicle ||
                    s.Status == ShipmentStatus.Dispatched ||
                    (s.Status == ShipmentStatus.Delivered && s.DeliveredAt.HasValue && s.DeliveredAt.Value.Date == today));

            if (driverId.HasValue)
            {
                query = query.Where(s => s.AssignedDriverId == driverId.Value);
            }

            var shipments = await query
                .OrderBy(s => s.Status == ShipmentStatus.Delivered ? 1 : 0) // active first
                    .ThenBy(s => s.DeliveryDate)
                .Select(s => new DriverShipmentDto
                {
                    Id                  = s.Id,
                    TalepNo             = s.TalepNo ?? s.IssOrder.TalepNo,
                    DeliveryDate        = s.DeliveryDate,
                    ProjectName         = s.Project.Name,
                    ProjectAddress      = s.Project.Address,
                    TeslimAlacakKisiler = s.IssOrder.TeslimAlacakKisiler,
                    TeslimAlacakTelefon = s.IssOrder.TeslimAlacakTelefonNumaralari,
                    DriverName          = s.AssignedDriverName,
                    PlateNumber         = s.AssignedPlateNumber,
                    Status              = s.Status.ToString(),
                    DeliveredAt         = s.DeliveredAt,
                    DeliveryPhotoBase64 = s.DeliveryPhotoBase64,
                    LineCount           = s.Lines.Count,
                })
                .ToListAsync(cancellationToken);

            return shipments;
        }
    }
}
