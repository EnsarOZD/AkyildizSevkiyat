using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Akyildiz.Sevkiyat.Domain.Enums;
using System;

namespace Akyildiz.Sevkiyat.Infrastructure.Persistence
{
    public class SevkiyatDbContext : DbContext, IApplicationDbContext
    {
        private readonly IPublisher _publisher;

        public SevkiyatDbContext(DbContextOptions<SevkiyatDbContext> options, IPublisher publisher)
            : base(options)
        {
            _publisher = publisher;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            base.OnConfiguring(optionsBuilder);
        }

        public DbSet<Project> Projects { get; set; } = null!;
        public DbSet<IssOrder> IssOrders { get; set; } = null!;
        public DbSet<IssOrderLine> IssOrderLines { get; set; } = null!;
        public DbSet<Shipment> Shipments { get; set; } = null!;
        public DbSet<ShipmentLine> ShipmentLines { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<ShipmentHistory> ShipmentHistories { get; set; } = null!;
        public DbSet<Zone> Zones { get; set; } = null!;
        public DbSet<StockMaster> StockMasters { get; set; } = null!;
        public DbSet<StockMapping> StockMappings { get; set; } = null!;
        public DbSet<ZonePreparation> ZonePreparations { get; set; } = null!;
        public DbSet<ZonePreparationProject> ZonePreparationProjects { get; set; } = null!;
        public DbSet<Driver> Drivers { get; set; } = null!;
        public DbSet<Vehicle> Vehicles { get; set; } = null!;
        public DbSet<ZonePreparationDriver> ZonePreparationDrivers { get; set; } = null!;
        public DbSet<StockTransaction> StockTransactions { get; set; } = null!;

        // NEW MODULE: Purchase Order & Goods Receipt
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; } = null!;
        public DbSet<PurchaseOrderLine> PurchaseOrderLines { get; set; } = null!;
        public DbSet<GoodsReceipt> GoodsReceipts { get; set; } = null!;
        public DbSet<GoodsReceiptLine> GoodsReceiptLines { get; set; } = null!;
        
        // Phase 1.1 Enhancements
        public DbSet<Supplier> Suppliers { get; set; } = null!;
        public DbSet<PurchaseOrderNumberCounter> PurchaseOrderNumberCounters { get; set; } = null!;
        public DbSet<FloatingReturn> FloatingReturns { get; set; } = null!;
        public DbSet<StockCount> StockCounts { get; set; } = null!;
        public DbSet<StockCountLine> StockCountLines { get; set; } = null!;

        // WMS Module
        public DbSet<WarehouseLocation> WarehouseLocations { get; set; } = null!;
        public DbSet<StockLocation> StockLocations { get; set; } = null!;
        public DbSet<LocationTransfer> LocationTransfers { get; set; } = null!;

        // Import Audit
        public DbSet<ImportBatch> ImportBatches { get; set; } = null!;
        public DbSet<ImportBatchOrder> ImportBatchOrders { get; set; } = null!;

        // Auth
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

        // Reconciliation
        public DbSet<ReconciliationIssue> ReconciliationIssues { get; set; } = null!;

        // System Settings
        public DbSet<SystemSettings> SystemSettings { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            // User
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Driver/Vehicle
            modelBuilder.Entity<Driver>().HasKey(d => d.Id);
            modelBuilder.Entity<Vehicle>().HasKey(v => v.Id);

            modelBuilder.Entity<StockMaster>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.StockCode).IsUnique();

                entity.Property(e => e.Unit).HasColumnType("int");
                entity.Property(e => e.Category).HasColumnType("int");
                entity.Property(e => e.TaxRate).HasColumnType("int");

                entity.Property(s => s.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(s => s.OnHandQty).HasColumnType("decimal(18,4)").HasDefaultValue(0m);
                entity.Property(s => s.ReservedQty).HasColumnType("decimal(18,4)").HasDefaultValue(0m);
                entity.Property(s => s.MinStockQty).HasColumnType("decimal(18,4)");
                entity.Property(s => s.WarehouseLocation).HasMaxLength(50);
                entity.Property(s => s.Brand).HasMaxLength(100);

                entity.Ignore(s => s.AvailableQty);

                // Optimistic concurrency
                entity.Property(s => s.RowVersion).IsRowVersion();

                // DB-level non-negative constraints
                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_StockMaster_OnHandQty_NonNegative",   "[OnHandQty] >= 0");
                    t.HasCheckConstraint("CK_StockMaster_ReservedQty_NonNegative", "[ReservedQty] >= 0");
                    t.HasCheckConstraint("CK_StockMaster_ReservedLteOnHand",       "[ReservedQty] <= [OnHandQty]");
                });
            });

            modelBuilder.Entity<StockTransaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.StockMasterId, e.Date });
                entity.Property(e => e.Qty).HasColumnType("decimal(18,4)");
                entity.Property(e => e.Reference).HasMaxLength(100);
                entity.Property(e => e.Note).HasMaxLength(500);

                entity.HasOne(e => e.StockMaster)
                    .WithMany()
                    .HasForeignKey(e => e.StockMasterId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // StockMapping Configuration
            modelBuilder.Entity<StockMapping>(entity =>
            {
                entity.HasIndex(m => m.ExternalStockCode);

                entity.HasOne(m => m.LocalStock)
                      .WithMany()
                      .HasForeignKey(m => m.LocalStockId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ZonePreparation Configuration
            modelBuilder.Entity<ZonePreparation>(entity =>
            {
                entity.HasIndex(e => new { e.ZoneId, e.DeliveryDate, e.BatchNo }).IsUnique();
                
                entity.HasOne(e => e.Zone)
                    .WithMany()
                    .HasForeignKey(e => e.ZoneId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.Projects)
                    .WithOne(p => p.ZonePreparation)
                    .HasForeignKey(p => p.ZonePreparationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Driver)
                    .WithMany()
                    .HasForeignKey(e => e.DriverId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Vehicle)
                    .WithMany()
                    .HasForeignKey(e => e.VehicleId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
            
            // Shipment -> ZonePreparation
            modelBuilder.Entity<Shipment>(entity =>
            {
                entity.HasIndex(s => s.DeliveryDate);
                entity.HasIndex(s => s.Status);
                entity.HasIndex(s => s.IssOrderId).IsUnique();

                // Optimistic concurrency
                entity.Property(s => s.RowVersion).IsRowVersion();

                entity.HasOne(s => s.ZonePreparation)
                .WithMany() // Or Add collection to ZonePreparation if needed
                .HasForeignKey(s => s.ZonePreparationId)
                .OnDelete(DeleteBehavior.SetNull);
            });

            // ZonePreparationProject Configuration
            modelBuilder.Entity<ZonePreparationProject>(entity =>
            {
                entity.HasOne(e => e.Project)
                    .WithMany()
                    .HasForeignKey(e => e.ProjectId)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            // Project: unique Code index
            modelBuilder.Entity<Project>(entity =>
            {
                entity.Property(e => e.Code).HasMaxLength(100).IsRequired();
                entity.HasIndex(e => e.Code).IsUnique();
            });

            // İLİŞKİLER
            // Project -> IssOrders
            modelBuilder.Entity<Project>()
                .HasMany(p => p.IssOrders)
                .WithOne(o => o.Project)
                .HasForeignKey(o => o.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // Project -> Shipments
            modelBuilder.Entity<Project>()
                .HasMany(p => p.Shipments)
                .WithOne(s => s.Project)
                .HasForeignKey(s => s.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // Project -> Zone
            modelBuilder.Entity<Project>()
                .HasOne(p => p.Zone)
                .WithMany(z => z.Projects)
                .HasForeignKey(p => p.ZoneId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<IssOrder>(entity =>
            {
                entity.HasMany(o => o.Lines)
                    .WithOne(l => l.IssOrder)
                    .HasForeignKey(l => l.IssOrderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(o => o.Status)
                    .HasConversion<string>();

                entity.Property(o => o.ExternalOrderNumber)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.HasIndex(o => o.ExternalOrderNumber)
                    .IsUnique();
            });

            // ImportBatch
            modelBuilder.Entity<ImportBatch>(entity =>
            {
                entity.HasMany(b => b.Orders)
                    .WithOne(o => o.ImportBatch)
                    .HasForeignKey(o => o.ImportBatchId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(b => b.Status)
                    .HasConversion<string>();
            });

            modelBuilder.Entity<ImportBatchOrder>(entity =>
            {
                entity.Property(o => o.ExternalOrderNumber).HasMaxLength(100).IsRequired();
                entity.Property(o => o.Action).HasMaxLength(20).IsRequired();
            });

            // IssOrderLine -> ShipmentLines
            modelBuilder.Entity<IssOrderLine>()
                .HasMany(l => l.ShipmentLines)
                .WithOne(sl => sl.IssOrderLine)
                .HasForeignKey(sl => sl.IssOrderLineId)
                .OnDelete(DeleteBehavior.Restrict);

            // Shipment -> ShipmentLines
            modelBuilder.Entity<Shipment>()
                .HasMany(s => s.Lines)
                .WithOne(sl => sl.Shipment)
                .HasForeignKey(sl => sl.ShipmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Shipment -> ShipmentHistories (Aggregate)
            modelBuilder.Entity<Shipment>()
                .HasMany(s => s.Histories)
                .WithOne()
                .HasForeignKey(h => h.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ShipmentHistory>()
                .HasIndex(h => new { h.ShipmentId, h.ChangedAt });

            // DECIMAL PRECISION
            modelBuilder.Entity<IssOrderLine>(entity =>
            {
                entity.Property(p => p.OrderedQty).HasColumnType("decimal(18,2)");
                entity.Property(p => p.BirimFiyati).HasColumnType("decimal(18,2)");
                entity.Property(p => p.ListeFiyati).HasColumnType("decimal(18,2)");
                entity.Property(p => p.Iskonto).HasColumnType("decimal(18,2)");
                entity.Property(p => p.KDVOrani).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<ShipmentLine>(entity =>
            {
                entity.Property(p => p.OrderedQty).HasColumnType("decimal(18,2)");
                entity.Property(p => p.DeliveredQty).HasColumnType("decimal(18,2)");
                entity.Property(p => p.ReturnedQty).HasColumnType("decimal(18,4)");

                entity.HasOne(sl => sl.StockMaster)
                    .WithMany()
                    .HasForeignKey(sl => sl.StockMasterId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<FloatingReturn>(entity =>
            {
                entity.Property(e => e.Qty).HasColumnType("decimal(18,4)");
                entity.Property(e => e.StockCodeFree).HasMaxLength(50);
                entity.Property(e => e.StockNameFree).HasMaxLength(200);
                entity.Property(e => e.Note).HasMaxLength(500);

                entity.HasOne(e => e.StockMaster)
                    .WithMany()
                    .HasForeignKey(e => e.StockMasterId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.LinkedShipment)
                    .WithMany()
                    .HasForeignKey(e => e.LinkedShipmentId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<StockCount>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Note).HasMaxLength(500);
                entity.HasMany(e => e.Lines)
                    .WithOne(l => l.StockCount)
                    .HasForeignKey(l => l.StockCountId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<StockCountLine>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ExpectedQty).HasColumnType("decimal(18,4)");
                entity.Property(e => e.ActualQty).HasColumnType("decimal(18,4)");
                entity.Property(e => e.Note).HasMaxLength(500);
                entity.Ignore(e => e.DifferenceQty);

                entity.HasOne(e => e.StockMaster)
                    .WithMany()
                    .HasForeignKey(e => e.StockMasterId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // WMS: StockLocation
            modelBuilder.Entity<StockLocation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.StockMasterId, e.WarehouseLocationId }).IsUnique();
                entity.Property(e => e.OnHandQty).HasColumnType("decimal(18,4)").HasDefaultValue(0m);
                entity.Property(e => e.ReservedQty).HasColumnType("decimal(18,4)").HasDefaultValue(0m);
                entity.Ignore(e => e.AvailableQty);

                entity.HasOne(e => e.StockMaster)
                    .WithMany()
                    .HasForeignKey(e => e.StockMasterId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.WarehouseLocation)
                    .WithMany()
                    .HasForeignKey(e => e.WarehouseLocationId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // WMS: LocationTransfer
            modelBuilder.Entity<LocationTransfer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.TransferredAt);
                entity.Property(e => e.Qty).HasColumnType("decimal(18,4)");
                entity.Property(e => e.Note).HasMaxLength(500);

                entity.HasOne(e => e.StockMaster)
                    .WithMany()
                    .HasForeignKey(e => e.StockMasterId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.FromLocation)
                    .WithMany()
                    .HasForeignKey(e => e.FromLocationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ToLocation)
                    .WithMany()
                    .HasForeignKey(e => e.ToLocationId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // WMS: WarehouseLocation
            modelBuilder.Entity<WarehouseLocation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Code).HasMaxLength(20);
                entity.Property(e => e.Taraf).HasMaxLength(1);
                entity.Property(e => e.Description).HasMaxLength(200);
                entity.Property(e => e.MaxWeightKg).HasColumnType("decimal(10,2)");
                entity.Property(e => e.LocationType).HasColumnType("int");
                entity.HasIndex(e => new { e.KoridorNo, e.Taraf, e.ModulNo, e.Kat });
            });

            // ReconciliationIssue
            modelBuilder.Entity<ReconciliationIssue>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.IssueKey).IsUnique();
                entity.HasIndex(e => new { e.Status, e.DetectedAt });
                entity.HasIndex(e => e.CheckType);
                entity.Property(e => e.IssueKey).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.ExpectedValue).HasMaxLength(200);
                entity.Property(e => e.ActualValue).HasMaxLength(200);
                entity.Property(e => e.AcknowledgementNote).HasMaxLength(500);
            });

            // RefreshToken
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TokenHash).HasMaxLength(64).IsRequired();
                entity.HasIndex(e => e.TokenHash).IsUnique();
                entity.HasIndex(e => new { e.UserId, e.RevokedAt });

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Vehicle
            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.Property(e => e.VehicleType)
                    .HasConversion<int>()
                    .HasDefaultValueSql("0");
                entity.Property(e => e.Description).HasMaxLength(500);
            });

            // ZonePreparationDriver
            modelBuilder.Entity<ZonePreparationDriver>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.ZonePreparation)
                    .WithMany(z => z.DriverAssignments)
                    .HasForeignKey(e => e.ZonePreparationId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Driver)
                    .WithMany()
                    .HasForeignKey(e => e.DriverId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.Property(e => e.IsPrimary).HasDefaultValue(false);
                entity.HasIndex(e => new { e.ZonePreparationId, e.DriverId }).IsUnique();
            });

            // SEED DATA
            // ---- Project ----
            modelBuilder.Entity<Project>().HasData(new Project
            {
                Id = 1,
                Code = "PRJ001",
                Name = "Kozyatağı Yemekhanesi",
                Region = "Anadolu-1",
                Address = "İstanbul / Kozyatağı",
                IsActive = true
            });

            // ---- IssOrder ----
            modelBuilder.Entity<IssOrder>().HasData(new IssOrder
            {
                Id = 1,
                ProjectId = 1,
                ExternalOrderNumber = "SO-1001",
                NetsisOrderNumber = null,
                OrderDate = DateTime.Today,
                DeliveryDate = DateTime.Today.AddDays(1),
                Status = IssOrderStatus.Imported
            });

            // ---- IssOrderLine ----
            modelBuilder.Entity<IssOrderLine>().HasData(new IssOrderLine
            {
                Id = 1,
                IssOrderId = 1,
                LineNumber = 1,
                StockCode = "EKMEK-01",
                StockName = "Somun Ekmek",
                Unit = StockUnit.Adet,
                OrderedQty = 120m
            });

            modelBuilder.Entity<StockMaster>().HasData(
                new StockMaster { Id = 1, StockCode = "EKMEK-01", StockName = "Somun Ekmek", IsActive = true },
                new StockMaster { Id = 2, StockCode = "SU-05", StockName = "Su 0.5L", IsActive = true },
                new StockMaster { Id = 3, StockCode = "YOGURT-200", StockName = "Yoğurt 200gr", IsActive = true }
            );

            // PurchaseOrder config moved to PurchaseOrderConfiguration.cs

            // ---- GoodsReceipt ----
            modelBuilder.Entity<GoodsReceipt>(entity =>
            {
               entity.HasKey(e => e.Id);
               
               // Weak Warning Index (Not Unique Constraint)
               entity.HasIndex(e => new { e.SupplierId, e.WaybillNo, e.WaybillDate });

               entity.HasOne(e => e.PurchaseOrder)
                   .WithMany()
                   .HasForeignKey(e => e.PurchaseOrderId)
                   .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<PurchaseOrderLine>(entity =>
            {
                entity.Property(e => e.OrderedQty).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<GoodsReceiptLine>(entity =>
            {
                entity.Property(e => e.OrderedQty).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ReceivedQty).HasColumnType("decimal(18,2)");
                entity.Property(e => e.AcceptedQty).HasColumnType("decimal(18,2)");
                entity.Property(e => e.RejectedQty).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.GoodsReceipt)
                    .WithMany(r => r.Lines)
                    .HasForeignKey(e => e.GoodsReceiptId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.PurchaseOrderLine)
                    .WithMany()
                    .HasForeignKey(e => e.PurchaseOrderLineId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.StockMaster)
                    .WithMany()
                    .HasForeignKey(e => e.StockMasterId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // SystemSettings — singleton row (Id=1 always, no identity)
            modelBuilder.Entity<SystemSettings>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.DepotName).HasMaxLength(200);
                entity.Property(e => e.DepotAddress).HasMaxLength(500);
            });

            // Project — TimeOnly columns stored as TIME
            modelBuilder.Entity<Project>(entity =>
            {
                entity.Property(e => e.DeliveryWindowStart).HasColumnType("time");
                entity.Property(e => e.DeliveryWindowEnd).HasColumnType("time");
            });
        }


        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var result = await base.SaveChangesAsync(cancellationToken);

            await DispatchDomainEvents(cancellationToken);

            return result;
        }

        private async Task DispatchDomainEvents(CancellationToken cancellationToken)
        {
            var entitiesWithEvents = ChangeTracker.Entries<IHasDomainEvents>()
                .Where(e => e.Entity.DomainEvents.Any())
                .Select(e => e.Entity)
                .ToList();

            foreach (var entity in entitiesWithEvents)
            {
                var events = entity.DomainEvents.ToArray();
                entity.ClearDomainEvents();

                foreach (var domainEvent in events)
                {
                    if (domainEvent is INotification notification)
                    {
                        await _publisher.Publish(notification, cancellationToken);
                    }
                }
            }
        }
    }
}
