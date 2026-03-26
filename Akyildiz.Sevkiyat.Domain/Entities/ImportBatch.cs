using Akyildiz.Sevkiyat.Domain.Enums;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    public class ImportBatch
    {
        public int Id { get; set; }
        public string Source { get; set; } = "ISS-IP";

        public DateTime RequestedStartDate { get; set; }
        public DateTime RequestedEndDate { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? StartedByUserId { get; set; }

        public ImportBatchStatus Status { get; set; } = ImportBatchStatus.Running;

        public int TotalFromSource { get; set; }
        public int NewCount { get; set; }
        public int SkippedCount { get; set; }
        public int NeedsMappingCount { get; set; }
        public int FailedCount { get; set; }
        public int DurationMs { get; set; }

        public string? ErrorSummary { get; set; }

        public ICollection<ImportBatchOrder> Orders { get; set; } = new List<ImportBatchOrder>();
    }

    public class ImportBatchOrder
    {
        public int Id { get; set; }
        public int ImportBatchId { get; set; }
        public ImportBatch ImportBatch { get; set; } = null!;

        public string ExternalOrderNumber { get; set; } = null!;
        public string Action { get; set; } = null!; // Created | Skipped | Failed
        public string? Warning { get; set; }
        public string? Error { get; set; }
        public int? IssOrderId { get; set; }
    }
}
