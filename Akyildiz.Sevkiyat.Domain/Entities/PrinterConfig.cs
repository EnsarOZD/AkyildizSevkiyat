using Akyildiz.Sevkiyat.Domain.Enums;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    /// <summary>Bir yazıcı tanımı — hangi agent'ta, hangi Windows yazıcısı, hangi etiket tipi.</summary>
    public class PrinterConfig
    {
        public int Id { get; set; }

        /// <summary>Kullanıcı dostu ad (ör. "Kargo Etiketi Yazıcısı").</summary>
        public string Name { get; set; } = string.Empty;

        public LabelType LabelType { get; set; }

        /// <summary>Windows'ta görünen yazıcı adı (ör. "ZDesigner LP 2844").</summary>
        public string WindowsPrinterName { get; set; } = string.Empty;

        public int? AgentId { get; set; }
        public PrintAgent? Agent { get; set; }

        /// <summary>Bu etiket tipi için varsayılan yazıcı mı?</summary>
        public bool IsDefault { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<PrintJob> PrintJobs { get; set; } = new List<PrintJob>();
    }
}
