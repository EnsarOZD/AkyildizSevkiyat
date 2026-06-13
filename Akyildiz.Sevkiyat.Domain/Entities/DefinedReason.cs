using Akyildiz.Sevkiyat.Domain.Enums;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    /// <summary>
    /// Yönetici tarafından tanımlanabilen sebep (fark/red) seçeneği.
    /// Etiket serbest metin olarak saklanır; mevcut kayıtlarla FK bağı yoktur.
    /// </summary>
    public class DefinedReason
    {
        public int Id { get; set; }
        public ReasonCategory Category { get; set; }
        public string Label { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
