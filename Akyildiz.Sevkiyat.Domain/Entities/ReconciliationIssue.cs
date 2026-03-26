using Akyildiz.Sevkiyat.Domain.Enums;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    /// <summary>
    /// Operasyonel tutarsızlık kaydı.
    /// Her sorun için benzersiz bir IssueKey atanır; tekrar çalışmalarda upsert için kullanılır.
    /// </summary>
    public class ReconciliationIssue
    {
        public int Id { get; set; }

        /// <summary>
        /// Doğal anahtar — upsert için. Format: "{CheckType}:{EntityRef}"
        /// Örnek: "IssQtyMismatch:SL123", "NetsisTransferMissing:S456"
        /// </summary>
        public string IssueKey { get; set; } = null!;

        public ReconciliationCheckType CheckType { get; set; }
        public ReconciliationSeverity Severity { get; set; }
        public ReconciliationStatus Status { get; set; } = ReconciliationStatus.Open;

        // --- Referans alanlar (null olabilir — check tipine göre dolu olan değişir) ---
        public int? ShipmentId { get; set; }
        public int? ShipmentLineId { get; set; }
        public int? IssOrderLineId { get; set; }

        // --- Sorun açıklaması ---
        public string Description { get; set; } = null!;

        /// <summary>Beklenen değer (insan okunabilir)</summary>
        public string? ExpectedValue { get; set; }

        /// <summary>Gerçek (hatalı) değer (insan okunabilir)</summary>
        public string? ActualValue { get; set; }

        // --- Yaşam döngüsü ---
        public DateTime DetectedAt { get; set; }

        public int? AcknowledgedByUserId { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public string? AcknowledgementNote { get; set; }
    }
}
