using Akyildiz.Sevkiyat.Domain.Enums;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    /// <summary>Baskı kuyruğu — her etiket baskı isteği bir iş kaydıdır.</summary>
    public class PrintJob
    {
        public int Id { get; set; }

        public int PrinterConfigId { get; set; }
        public PrinterConfig PrinterConfig { get; set; } = null!;

        public LabelType LabelType { get; set; }

        /// <summary>Etiket verisi — JSON (kargo için: barcode, alıcı, adres, irsaliye no vb.).</summary>
        public string PayloadJson { get; set; } = string.Empty;

        public PrintJobStatus Status { get; set; } = PrintJobStatus.Pending;

        public string? ErrorMessage { get; set; }

        /// <summary>Kopiyi isteyen kullanıcı (opsiyonel).</summary>
        public int? CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
