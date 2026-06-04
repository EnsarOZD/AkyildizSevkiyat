namespace Akyildiz.Sevkiyat.Domain.Entities
{
    /// <summary>
    /// ISS senkronizasyonunda bir projenin adresi değiştiğinde tutulan denetim kaydı.
    /// "Şu proje kodunun adresi değişti: eski → yeni" uyarısı ve veri kalitesi için.
    /// </summary>
    public class ProjectAddressChange
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectCode { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string? OldAddress { get; set; }
        public string? NewAddress { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}
