using System.Security.Cryptography;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    /// <summary>
    /// Nakliye ile gönderilen bir projeye ait teslimat için, nakliyecinin login olmadan
    /// teslim fotoğrafı yükleyebileceği token'lı public link kaydı. Proje bazında oluşturulur
    /// (bir nakliyeci birden fazla projeye teslim edebilir → her proje için ayrı link).
    /// </summary>
    public class FreightDelivery
    {
        public Guid Id { get; private set; }
        public string Token { get; private set; } = null!;

        public int ProjectId { get; private set; }
        public Project Project { get; private set; } = null!;

        public string CarrierName { get; private set; } = null!;
        public string? CarrierPhone { get; private set; }

        public DateTime CreatedAt { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public DateTime? CompletedAt { get; private set; }

        public string? RecipientName { get; private set; }
        public string? Note { get; private set; }

        public ICollection<FreightDeliveryShipment> Shipments { get; private set; } = new List<FreightDeliveryShipment>();

        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
        public bool IsCompleted => CompletedAt.HasValue;

        private FreightDelivery() { }

        public static FreightDelivery Create(int projectId, string carrierName, string? carrierPhone, int validHours = 72)
        {
            return new FreightDelivery
            {
                Id = Guid.NewGuid(),
                Token = GenerateToken(),
                ProjectId = projectId,
                CarrierName = carrierName,
                CarrierPhone = carrierPhone,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(validHours),
            };
        }

        public void AddShipment(int shipmentId)
            => Shipments.Add(FreightDeliveryShipment.Create(Id, shipmentId));

        public void Complete(string recipientName, string? note)
        {
            CompletedAt = DateTime.UtcNow;
            RecipientName = recipientName;
            Note = note;
        }

        /// <summary>URL-güvenli, tahmin edilemez token (~32 karakter).</summary>
        private static string GenerateToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(24);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }
    }
}
