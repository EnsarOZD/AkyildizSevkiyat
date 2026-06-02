using System.Collections.Generic;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    public class Zone
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int Order { get; set; } // Picking route order
        public bool IsOutOfCity { get; set; } = false;

        /// <summary>
        /// Pasif bölgeler operasyonel seçim listelerinde görünmez (soft-delete).
        /// Hazırlık kayıtları olduğu için fiziksel silinemeyen bölgeler pasife alınır.
        /// </summary>
        public bool IsActive { get; set; } = true;

        public ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}
