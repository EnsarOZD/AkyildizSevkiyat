namespace Akyildiz.Sevkiyat.Domain.Enums
{
    /// <summary>
    /// Proje bazlı operasyon tipi — picking akışı ve iş kuralları bu tipe göre ayrışır.
    /// </summary>
    public enum OperationType
    {
        /// <summary>
        /// Catering / İkram operasyonu.
        /// Mevcut akış: Micro (proje bazında) + Macro (zone bazında gruplu) picking.
        /// </summary>
        Catering = 0,

        /// <summary>
        /// Kıyafet operasyonu.
        /// Picking akışı henüz tanımlanmadı. Hazırlık: Project.OperationType alanı mevcuttur.
        /// İleride: beden/renk bazlı toplama, barkod tarama, farklı gruplandırma mantığı.
        /// </summary>
        Clothing = 1,
    }
}
