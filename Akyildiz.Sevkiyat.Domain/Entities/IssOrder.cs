using Akyildiz.Sevkiyat.Domain.Enums;
using System;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    public class IssOrder
    {
        public int Id { get; set; }

        public string ExternalOrderNumber { get; set; } = null!; // ISS-IP sipariş no
        public string? NetsisOrderNumber { get; set; }           // İleride kullanırız

        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public DateTime OrderDate { get; set; }      // Sipariş tarihi
        public DateTime DeliveryDate { get; set; }   // Planlanan teslim günü

        public IssOrderStatus Status { get; set; }   // Imported, Cancelled vs.
        
        // New Fields from Web Service
        public string? TalepNo { get; set; }
        public string? TeslimAlacakKisiler { get; set; }
        public string? TeslimAlacakTelefonNumaralari { get; set; }
        public string? TalepTuru { get; set; }
        public string? Aciklama { get; set; }
        public string? Donem { get; set; }
        public string? YoneticiMailAdresleri { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsTransferred { get; set; } = false; // Added to track shipment status

        public ImportStatus ImportStatus { get; set; } // Ready, NeedsMapping

        public ICollection<IssOrderLine> Lines { get; set; } = new List<IssOrderLine>();
    }
}
