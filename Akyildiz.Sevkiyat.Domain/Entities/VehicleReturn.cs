namespace Akyildiz.Sevkiyat.Domain.Entities
{
    public class VehicleReturn
    {
        public int Id { get; set; }
        public Guid DriverSessionId { get; set; }
        public DriverSession DriverSession { get; set; } = null!;
        public DateTime ReturnDate { get; set; }
        public string? Note { get; set; }
        public int? CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<VehicleReturnLine> Lines { get; set; } = new();
    }
}
