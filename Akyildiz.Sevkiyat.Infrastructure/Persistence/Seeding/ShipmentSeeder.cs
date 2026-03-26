using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Akyildiz.Sevkiyat.Infrastructure.Persistence.Seeding
{
    public static class ShipmentSeeder
    {
        public static async Task SeedAsync(IApplicationDbContext context)
        {
            if (!await context.Shipments.AnyAsync())
            {
                // 1. Create Project
                var project = new Project
                {
                    Code = "PROJ-001",
                    Name = "Mecidiyeköy Ofis Projesi",
                    Region = "Avrupa",
                    Address = "Büyükdere Cad. No:199",
                    IsActive = true
                };

                // 2. Create IssOrder
                var issOrder = new IssOrder
                {
                    Project = project,
                    ExternalOrderNumber = "SIP-1001",
                    OrderDate = DateTime.Now.AddDays(-5),
                    DeliveryDate = DateTime.Now.AddDays(2),
                    Status = IssOrderStatus.Imported
                };

                // 3. Create IssOrderLines
                var issOrderLine1 = new IssOrderLine
                {
                    IssOrder = issOrder,
                    LineNumber = 10,
                    StockCode = "MS-001",
                    StockName = "Çalışma Masası",
                    Unit = StockUnit.Adet,
                    OrderedQty = 10
                };

                 var issOrderLine2 = new IssOrderLine
                {
                    IssOrder = issOrder,
                    LineNumber = 20,
                    StockCode = "SD-002",
                    StockName = "Ofis Sandalyesi",
                    Unit = StockUnit.Adet,
                    OrderedQty = 20
                };

                context.Projects.Add(project); // Adds dependencies (IssOrder, Lines)

                // 4. Create Shipment
                var shipment = new Shipment
                {
                    Project = project,
                    IssOrder = issOrder,
                    DeliveryDate = DateTime.Now.AddDays(2),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                };

                // 5. Create ShipmentLines using factory method
                var shipmentLine1 = ShipmentLine.Create(
                    issOrderLine1.Id,
                    null,
                    issOrderLine1.StockCode,
                    issOrderLine1.StockName,
                    issOrderLine1.Unit,
                    issOrderLine1.OrderedQty);

                var shipmentLine2 = ShipmentLine.Create(
                    issOrderLine2.Id,
                    null,
                    issOrderLine2.StockCode,
                    issOrderLine2.StockName,
                    issOrderLine2.Unit,
                    issOrderLine2.OrderedQty);

                shipment.Lines.Add(shipmentLine1);
                shipment.Lines.Add(shipmentLine2);

                // Set Status to AssignedToWarehouse (Sample)
                // Using reflection for private set
                typeof(Shipment).GetProperty(nameof(Shipment.Status))?
                    .SetValue(shipment, ShipmentStatus.AssignedToWarehouse);


                // Add to context
                context.Shipments.Add(shipment);

                var project2 = new Project
                {
                    Code = "PROJ-002",
                    Name = "Ankara Hastane İnşaatı",
                    Region = "Anadolu",
                    Address = "Ankara",
                    IsActive = true
                };

                var issOrder2 = new IssOrder
                {
                    Project = project2,
                    ExternalOrderNumber = "SIP-1002",
                    OrderDate = DateTime.Now.AddDays(-2),
                    DeliveryDate = DateTime.Now.AddDays(10),
                    Status = IssOrderStatus.Imported
                };

                var issOrderLine2_1 = new IssOrderLine
                {
                    IssOrder = issOrder2,
                    LineNumber = 10,
                    StockCode = "YT-005",
                    StockName = "Hasta Yatağı",
                    Unit = StockUnit.Adet,
                    OrderedQty = 50
                };

                var shipment2_real = new Shipment
                {
                    Project = project2,
                    IssOrder = issOrder2,
                    DeliveryDate = DateTime.Now.AddDays(5),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                };
                // Status default is Created

                var shipmentLine2_1 = ShipmentLine.Create(
                    issOrderLine2_1.Id,
                    null,
                    issOrderLine2_1.StockCode,
                    issOrderLine2_1.StockName,
                    issOrderLine2_1.Unit,
                    issOrderLine2_1.OrderedQty);

                shipment2_real.Lines.Add(shipmentLine2_1);

                context.Projects.Add(project2);
                context.Shipments.Add(shipment2_real);

                await context.SaveChangesAsync(CancellationToken.None);
            }
        }
    }
}
