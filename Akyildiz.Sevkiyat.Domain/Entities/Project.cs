using Akyildiz.Sevkiyat.Domain.Enums;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    public class Project
    {
        public int Id { get; set; }

        public string Code { get; set; } = null!;   // ISS proje kodu (varsa) -> Aslinda ProjeKodu
        public string? InstitutionCode { get; set; } // KurumKodu
        public string Name { get; set; } = null!;

        /// <summary>
        /// Proje operasyon tipi — picking akışını ve iş kurallarını belirler.
        /// Catering (varsayılan): Micro + Macro zone picking.
        /// Clothing: Beden/renk bazlı picking (henüz tanımlanmadı — ileride uygulanacak).
        /// </summary>
        public OperationType OperationType { get; set; } = OperationType.Catering;
        public string? Region { get; set; }         // Avrupa1, Anadolu2 vb.
        public string? Address { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<IssOrder> IssOrders { get; set; } = new List<IssOrder>();
        public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
        
        public int? ZoneId { get; set; }
        public Zone? Zone { get; set; }

        public DateTime? LastSyncedAt { get; set; }

        // Netsis cari kodu — API bilgileri gelince doldurulur
        public string? NetsisCariKodu { get; set; }

        /// <summary>
        /// Şoför rota sırası — bölge içindeki ziyaret önceliği.
        /// Null = sıra belirlenmemiş (bölge sırası + alfabetik ile sıralanır).
        /// </summary>
        public int? DeliveryOrder { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public TimeOnly? DeliveryWindowStart { get; set; }
        public TimeOnly? DeliveryWindowEnd { get; set; }
    }
}
