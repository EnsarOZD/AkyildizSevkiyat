using Akyildiz.Sevkiyat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Akyildiz.Sevkiyat.Infrastructure.Persistence.Configurations
{
    public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
    {
        public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.OrderNumber)
                .IsRequired()
                .HasMaxLength(20);
            
            builder.HasIndex(x => x.OrderNumber)
                .IsUnique();

            builder.HasOne(x => x.Supplier)
                .WithMany()
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.EmailSentTo).HasMaxLength(500);

            builder.HasMany(x => x.Lines)
                .WithOne(x => x.PurchaseOrder)
                .HasForeignKey(x => x.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class PurchaseOrderNumberCounterConfiguration : IEntityTypeConfiguration<PurchaseOrderNumberCounter>
    {
        public void Configure(EntityTypeBuilder<PurchaseOrderNumberCounter> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => new { x.Year, x.Month })
                .IsUnique();
        }
    }
    
    public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
    {
        public void Configure(EntityTypeBuilder<Supplier> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
            builder.HasIndex(x => x.Name); // Search index
        }
    }
}
