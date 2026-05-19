namespace Akyildiz.Sevkiyat.Domain.Entities
{
    /// <summary>Depodaki PC'de çalışan yerel baskı ajanı.</summary>
    public class PrintAgent
    {
        public int Id { get; set; }

        /// <summary>Agent'ın appsettings.json'unda saklanan GUID — kimlik doğrulama için.</summary>
        public string AgentKey { get; set; } = string.Empty;

        /// <summary>Makine adı (Environment.MachineName).</summary>
        public string MachineName { get; set; } = string.Empty;

        /// <summary>Kullanıcı tarafından verilen görüntü adı (ör. "Ana Depo").</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Agent'ın son heartbeat/kayıt zamanı.</summary>
        public DateTime LastSeenAt { get; set; }

        /// <summary>Agent'ta kurulu Windows yazıcı adları (JSON dizisi).</summary>
        public string? InstalledPrintersJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<PrinterConfig> PrinterConfigs { get; set; } = new List<PrinterConfig>();
    }
}
