using MediatR;
using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using System.Collections.Generic;
using System;

namespace Akyildiz.Sevkiyat.Application.Shipments.Queries.GetShipmentDetail
{
    public sealed record GetShipmentDetailQuery(int Id) : IRequest<ShipmentDetailDto>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Warehouse", "Dispatcher", "Accounting" };
    }

    public class ShipmentDetailDto
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public int? ZoneId { get; set; } // Added
        public string? ZoneName { get; set; } // Added
        public string Status { get; set; } = string.Empty;
        public DateTime DeliveryDate { get; set; }
        public string? DriverName { get; set; }
        public string? PlateNumber { get; set; }

        // Netsis / İrsaliye
        public string? IrsaliyeNo { get; set; }
        public DateOnly? IrsaliyeDate { get; set; }
        public DateTime? NetsisTransferredAt { get; set; }

        // Delivery Proof
        public DateTime? DeliveredAt { get; set; }
        public string? DeliveryNote { get; set; }
        public string? DeliveryRecipient { get; set; }
        public string? DeliveryPhotoBase64 { get; set; }

        // New Info Fields
        public string? ExternalOrderNumber { get; set; }
        public string? TalepNo { get; set; }
        public string? TeslimAlacakKisiler { get; set; }
        public string? TeslimAlacakTelefon { get; set; }
        public string? YoneticiMail { get; set; }
        public string? Aciklama { get; set; }
        public string OperationType { get; set; } = "Catering";
        public int OperationTypeValue { get; set; } = 0;

        public List<ShipmentLineDetailDto> Lines { get; set; } = new();
        public List<ShipmentHistoryDto> History { get; set; } = new();
    }

    public class ShipmentLineDetailDto
    {
        public int Id { get; set; }
        public string StockCode { get; set; } = string.Empty;
        public string StockName { get; set; } = string.Empty;
        public decimal OrderedQty { get; set; }
        public decimal DeliveredQty { get; set; }
        public string? DifferenceReason { get; set; }
        public string? Note { get; set; }
        
        public string LocalStockCode { get; set; } = string.Empty; // Added
        public string? Unit { get; set; } // Added
        public decimal? StockQty { get; set; } // Added (Placeholder)
        
        public string? ZoneName { get; set; }
        public int? ZoneOrder { get; set; }
    }

    public class ShipmentHistoryDto
    {
        public string OldStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string ChangedBy { get; set; } = string.Empty;
        public string? Description { get; set; } // Added
    }
}
