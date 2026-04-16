using Microsoft.EntityFrameworkCore;
using Akyildiz.Sevkiyat.Domain.Entities;

namespace Akyildiz.Sevkiyat.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<Project> Projects { get; }
        DbSet<IssOrder> IssOrders { get; }
        DbSet<IssOrderLine> IssOrderLines { get; }
        DbSet<Shipment> Shipments { get; }
        DbSet<ShipmentLine> ShipmentLines { get; }
        DbSet<User> Users { get; }
        DbSet<ShipmentHistory> ShipmentHistories { get; }
        DbSet<Zone> Zones { get; }
        DbSet<StockMaster> StockMasters { get; }
        DbSet<StockMapping> StockMappings { get; }
        DbSet<ZonePreparation> ZonePreparations { get; }
        DbSet<ZonePreparationProject> ZonePreparationProjects { get; }
        DbSet<Akyildiz.Sevkiyat.Domain.Entities.Driver> Drivers { get; }
        DbSet<Vehicle> Vehicles { get; }
        DbSet<ZonePreparationDriver> ZonePreparationDrivers { get; }
        DbSet<DriverSession> DriverSessions { get; }
        DbSet<StockTransaction> StockTransactions { get; }
        
        // PurchaseOrder & GoodsReceipt Module
        DbSet<PurchaseOrder> PurchaseOrders { get; }
        DbSet<PurchaseOrderLine> PurchaseOrderLines { get; }
        DbSet<GoodsReceipt> GoodsReceipts { get; }
        DbSet<GoodsReceiptLine> GoodsReceiptLines { get; }

        DbSet<Supplier> Suppliers { get; }
        DbSet<PurchaseOrderNumberCounter> PurchaseOrderNumberCounters { get; }
        DbSet<FloatingReturn> FloatingReturns { get; }
        DbSet<StockCount> StockCounts { get; }
        DbSet<StockCountLine> StockCountLines { get; }

        // Import Audit
        DbSet<ImportBatch> ImportBatches { get; }
        DbSet<ImportBatchOrder> ImportBatchOrders { get; }

        // Reconciliation
        DbSet<ReconciliationIssue> ReconciliationIssues { get; }

        DbSet<Akyildiz.Sevkiyat.Domain.Entities.SystemSettings> SystemSettings { get; }

        // Auth
        DbSet<RefreshToken> RefreshTokens { get; }

        // Print Audit
        DbSet<ShipmentPrintLog> ShipmentPrintLogs { get; }

        // WMS Module
        DbSet<WarehouseLocation> WarehouseLocations { get; }
        DbSet<StockLocation> StockLocations { get; }
        DbSet<LocationTransfer> LocationTransfers { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
