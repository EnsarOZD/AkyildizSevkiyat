using Akyildiz.Sevkiyat.Domain.Common;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    /// <summary>
    /// Nakliyeci (taşeron taşıyıcı) tanımı. Nakliye ile gönderimde seçilip alanları
    /// (ad/telefon/plaka) otomatik doldurmak için kullanılır. Gönderimde değerler
    /// Shipment/FreightDelivery üzerine snapshot olarak kopyalanır — burası yalnızca
    /// lookup kaynağıdır.
    /// </summary>
    public class Carrier : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;   // Ad / Ünvan
        public string? Phone { get; set; }
        public string? City { get; set; }                    // İl / Bölge — sonraki sevkiyatlarda iletişim için
        public bool IsActive { get; set; } = true;

        public ICollection<CarrierVehicle> Vehicles { get; set; } = new List<CarrierVehicle>();
    }
}
