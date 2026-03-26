using System.Collections.Generic;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    public class Zone
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int Order { get; set; } // Picking route order
        
        public ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}
