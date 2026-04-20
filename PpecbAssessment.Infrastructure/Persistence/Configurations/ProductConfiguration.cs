using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PpecbAssessment.Domain.Entities;

namespace PpecbAssessment.Infrastructure.Persistence.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.HasKey(x => x.ProductId);

            builder.Property(x => x.ProductCode)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Description)
                .HasMaxLength(1000);

            builder.Property(x => x.Price)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.ImagePath)
                .HasMaxLength(500);

            builder.Property(x => x.UserId)
                .IsRequired()
                .HasMaxLength(450);

            builder.HasIndex(x => new { x.UserId, x.ProductCode })
                .IsUnique();

            builder.Property(x => x.RowVersion)
                .IsRowVersion();

            builder.HasOne(x => x.Category)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
