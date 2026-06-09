using Akyildiz.Sevkiyat.Domain.Enums;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    /// <summary>Toplama kabı — araba veya palet. Code = QR/barkod değeri.</summary>
    public class Container
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;   // QR/barkod
        public PickingContainerType Type { get; set; } = PickingContainerType.Cart;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Bir kabın bir sevkiyata bağlanma kaydı (geçmiş dahil). ReleasedAt boş = aktif bağ.</summary>
    public class ContainerAssignment
    {
        public int Id { get; set; }
        public int ContainerId { get; set; }
        public Container Container { get; set; } = null!;
        public int ShipmentId { get; set; }
        public Shipment Shipment { get; set; } = null!;
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReleasedAt { get; set; }
        public int? AssignedByUserId { get; set; }
        public string? ReleaseReason { get; set; }   // Manuel boşaltmada zorunlu
    }
}
