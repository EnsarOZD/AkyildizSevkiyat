using Akyildiz.Sevkiyat.Application.Common.Dtos;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Common.Services
{
    /// <summary>
    /// Nakliye gönderiminde sevkiyatları proje bazında gruplayıp her proje için
    /// bir FreightDelivery (public teslim linki) oluşturur. Şoför akışındaki gibi
    /// proje bazında ayrı link üretilir.
    /// </summary>
    public static class FreightDeliveryFactory
    {
        public static async Task<List<FreightDeliveryLinkDto>> CreateForShipmentsAsync(
            IApplicationDbContext context,
            IEnumerable<Shipment> shipments,
            string carrierName,
            string? carrierPhone,
            CancellationToken cancellationToken)
        {
            var groups = shipments.GroupBy(s => s.ProjectId).ToList();
            if (groups.Count == 0) return new List<FreightDeliveryLinkDto>();

            var projectIds = groups.Select(g => g.Key).ToList();
            var projectNames = await context.Projects
                .Where(p => projectIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

            var links = new List<FreightDeliveryLinkDto>();
            foreach (var group in groups)
            {
                var delivery = FreightDelivery.Create(group.Key, carrierName, carrierPhone);
                foreach (var shipment in group)
                    delivery.AddShipment(shipment.Id);

                context.FreightDeliveries.Add(delivery);

                links.Add(new FreightDeliveryLinkDto(
                    group.Key,
                    projectNames.GetValueOrDefault(group.Key, string.Empty),
                    delivery.Token,
                    carrierPhone,
                    group.Count()));
            }

            return links;
        }
    }
}
