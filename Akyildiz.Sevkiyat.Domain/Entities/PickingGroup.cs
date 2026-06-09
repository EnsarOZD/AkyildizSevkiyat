namespace Akyildiz.Sevkiyat.Domain.Entities
{
    /// <summary>
    /// Müdür tarafından yönetilen toplama grubu (ör. "Güvenlik", "Küçük/Poşet").
    /// Sabit enum yerine veriyle yönetilir; sevkiyatlar bu gruplara dağıtılır,
    /// toplayıcılar grup içinden iş claim eder.
    /// </summary>
    public class PickingGroup
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
