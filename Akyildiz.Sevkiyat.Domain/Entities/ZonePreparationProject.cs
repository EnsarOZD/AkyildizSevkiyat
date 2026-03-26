using System;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    public class ZonePreparationProject
    {
        public int Id { get; set; }
        
        public int ZonePreparationId { get; set; }
        public ZonePreparation ZonePreparation { get; set; } = null!;

        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public bool IsMicroReady { get; set; } = false;
        public DateTime? MicroReadyAt { get; set; }
        
        public bool IsAddedLater { get; set; } = false;
    }
}
