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
        /// <summary>Kıyafet operasyonlarını hariç tutar — tüm depo pipeline handler'ları bunu kullanır.</summary>
        IQueryable<Shipment> WarehouseShipments { get; }
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
        DbSet<DriverSessionShipment> DriverSessionShipments { get; }
        DbSet<FreightDelivery> FreightDeliveries { get; }
        DbSet<FreightDeliveryShipment> FreightDeliveryShipments { get; }
        DbSet<Carrier> Carriers { get; }
        DbSet<CarrierVehicle> CarrierVehicles { get; }
        DbSet<ProjectAddressChange> ProjectAddressChanges { get; }
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

        // Delivery Photos
        DbSet<ShipmentDeliveryPhoto> ShipmentDeliveryPhotos { get; }

        // Stok Tüketim / Zai
        DbSet<StockConsumption> StockConsumptions { get; }

        // WMS Module
        DbSet<WarehouseLocation> WarehouseLocations { get; }
        DbSet<StockLocation> StockLocations { get; }
        DbSet<LocationTransfer> LocationTransfers { get; }

        // External Email Contacts
        DbSet<ExternalEmailContact> ExternalEmailContacts { get; }

        // Kıyafet toplama vurgu anahtar kelimeleri
        DbSet<ClothingHighlightKeyword> ClothingHighlightKeywords { get; }

        // Kıyafet toplama/kapama V1
        DbSet<PickingGroup> PickingGroups { get; }
        DbSet<ShortageRecord> ShortageRecords { get; }
        DbSet<Container> Containers { get; }
        DbSet<ContainerAssignment> ContainerAssignments { get; }

        // ISS KurumKodu → Netsis Fatura Cari Kodu eşleşmeleri
        DbSet<InstitutionCariMapping> InstitutionCariMappings { get; }

        // Vehicle Return Tracking
        DbSet<VehicleReturn> VehicleReturns { get; }
        DbSet<VehicleReturnLine> VehicleReturnLines { get; }

        // Notifications
        DbSet<Domain.Entities.Notification> Notifications { get; }
        DbSet<Domain.Entities.PushSubscription> PushSubscriptions { get; }

        // Print Queue
        DbSet<Domain.Entities.PrintAgent> PrintAgents { get; }
        DbSet<Domain.Entities.PrinterConfig> PrinterConfigs { get; }
        DbSet<Domain.Entities.PrintJob> PrintJobs { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
