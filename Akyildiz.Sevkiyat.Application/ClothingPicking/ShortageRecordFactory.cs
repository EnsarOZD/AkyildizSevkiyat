using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;

namespace Akyildiz.Sevkiyat.Application.ClothingPicking
{
    internal static class ShortageRecordFactory
    {
        /// <summary>
        /// Kapama/hazırlık tamamlanırken DeliveredQty &lt; OrderedQty olan her satır için
        /// ShortageRecord(Pending) üretir. shipment.Lines ve shipment.Project YÜKLÜ olmalı.
        /// </summary>
        public static List<ShortageRecord> CreateForShipment(Shipment shipment, int? userId)
        {
            var result = new List<ShortageRecord>();
            foreach (var l in shipment.Lines)
            {
                var shortage = l.OrderedQty - l.DeliveredQty;
                if (shortage <= 0) continue;

                result.Add(new ShortageRecord
                {
                    ShipmentId = shipment.Id,
                    ShipmentLineId = l.Id,
                    StockMasterId = l.StockMasterId,
                    StockCode = l.StockCode,
                    StockName = l.StockName,
                    ProjectId = shipment.ProjectId,
                    ProjectName = shipment.Project?.Name ?? string.Empty,
                    Qty = shortage,
                    Note = l.DifferenceReason,
                    Status = ShortageStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = userId,
                });
            }
            return result;
        }
    }
}
