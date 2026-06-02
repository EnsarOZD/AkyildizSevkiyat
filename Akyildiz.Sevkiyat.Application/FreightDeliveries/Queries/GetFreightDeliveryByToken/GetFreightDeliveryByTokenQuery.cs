using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.FreightDeliveries.Queries.GetFreightDeliveryByToken
{
    public record GetFreightDeliveryByTokenQuery(string Token) : IRequest<FreightDeliveryInfoDto>;

    public record FreightDeliveryInfoDto(
        string ProjectName,
        string CarrierName,
        bool IsCompleted,
        bool IsExpired,
        DateTime ExpiresAt,
        string? RecipientName,
        List<FreightDeliveryShipmentInfoDto> Shipments
    );

    public record FreightDeliveryShipmentInfoDto(
        int ShipmentId,
        string? IrsaliyeNo,
        string? TalepNo,
        int LineCount
    );

    public class GetFreightDeliveryByTokenQueryHandler
        : IRequestHandler<GetFreightDeliveryByTokenQuery, FreightDeliveryInfoDto>
    {
        private readonly IApplicationDbContext _context;

        public GetFreightDeliveryByTokenQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<FreightDeliveryInfoDto> Handle(
            GetFreightDeliveryByTokenQuery request,
            CancellationToken cancellationToken)
        {
            var delivery = await _context.FreightDeliveries
                .AsNoTracking()
                .Include(d => d.Project)
                .Include(d => d.Shipments).ThenInclude(s => s.Shipment).ThenInclude(s => s.Lines)
                .FirstOrDefaultAsync(d => d.Token == request.Token, cancellationToken)
                ?? throw new NotFoundException("Teslim linki bulunamadı veya geçersiz.");

            return new FreightDeliveryInfoDto(
                delivery.Project.Name,
                delivery.CarrierName,
                delivery.IsCompleted,
                delivery.IsExpired,
                delivery.ExpiresAt,
                delivery.RecipientName,
                delivery.Shipments
                    .Select(s => new FreightDeliveryShipmentInfoDto(
                        s.ShipmentId,
                        s.Shipment.IrsaliyeNo,
                        s.Shipment.TalepNo,
                        s.Shipment.Lines.Count))
                    .ToList());
        }
    }
}
