using System.Collections.Generic;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    public class Driver
    {
        public int Id { get; set; }
        public string FullName { get; set; } = null!;
        public string? Phone { get; set; }
        public bool IsActive { get; set; } = true;

        public int? UserId { get; set; }
        public User? User { get; set; }
    }
}
