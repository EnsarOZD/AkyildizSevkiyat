using Akyildiz.Sevkiyat.Domain.Common;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    /// <summary>
    /// ISS-IP'den gelen KurumKodu → Netsis Fatura Cari Kodu eşleşmesi.
    /// Yeni proje ISS sipariş import'unda otomatik açıldığında, KurumKodu'na karşılık gelen
    /// NetsisCariKodu bu tablodan okunup proje kartına yazılır. Mevcut projeler de bu eşleşmeye
    /// göre toplu olarak senkronize edilebilir (sistem bütünlüğü).
    /// </summary>
    public class InstitutionCariMapping : AuditableEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// ISS-IP siparişinden gelen string KurumKodu (örn. ana cari ayırt edici kod).
        /// Aynı kurum kodu sadece bir kez tanımlanabilir (unique).
        /// </summary>
        public string InstitutionCode { get; set; } = null!;

        /// <summary>
        /// Hedef Netsis Fatura Cari Kodu (örn. 120.01.001).
        /// </summary>
        public string NetsisCariKodu { get; set; } = null!;

        /// <summary>
        /// İnsan okunabilir açıklama — operatör için (örn. "ISS Catering", "Proser").
        /// </summary>
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
