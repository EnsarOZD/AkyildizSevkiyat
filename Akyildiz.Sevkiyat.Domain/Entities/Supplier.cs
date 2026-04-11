using Akyildiz.Sevkiyat.Domain.Common;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    public class Supplier : AuditableEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? SupplierCode { get; set; } // Netsis Code
        public string? Email { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
