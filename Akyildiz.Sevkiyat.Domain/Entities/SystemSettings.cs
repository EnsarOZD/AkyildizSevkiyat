namespace Akyildiz.Sevkiyat.Domain.Entities
{
    /// <summary>Singleton sistem ayarları (Id=1 her zaman).</summary>
    public class SystemSettings
    {
        public int Id { get; set; } = 1;
        public string? DepotName { get; set; }
        public string? DepotAddress { get; set; }
        public double? DepotLatitude { get; set; }
        public double? DepotLongitude { get; set; }
    }
}
