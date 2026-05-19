using Akyildiz.Sevkiyat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Akyildiz.Sevkiyat.Infrastructure.Persistence.Configurations
{
    public class StockConsumptionConfiguration : IEntityTypeConfiguration<StockConsumption>
    {
        public void Configure(EntityTypeBuilder<StockConsumption> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.StockCodeSnapshot).IsRequired().HasMaxLength(100);
            builder.Property(x => x.StockNameSnapshot).IsRequired().HasMaxLength(300);
            builder.Property(x => x.Quantity).HasColumnType("decimal(18,4)");
            builder.Property(x => x.SalePrice).HasColumnType("decimal(18,4)");
            builder.Property(x => x.Reason).HasMaxLength(500);
            builder.Property(x => x.RecipientName).HasMaxLength(200);
            builder.Property(x => x.Note).HasMaxLength(500);

            builder.HasOne(x => x.StockMaster)
                .WithMany()
                .HasForeignKey(x => x.StockMasterId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.Date);
            builder.HasIndex(x => x.Type);
            builder.HasIndex(x => x.StockMasterId);
        }
    }
}
