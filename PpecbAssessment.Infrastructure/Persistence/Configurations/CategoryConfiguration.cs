using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PpecbAssessment.Domain.Entities;

namespace PpecbAssessment.Infrastructure.Persistence.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.HasKey(x => x.CategoryId);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.CategoryCode)
                .IsRequired()
                .HasMaxLength(6);

            builder.Property(x => x.UserId)
                .IsRequired()
                .HasMaxLength(450);

            builder.HasIndex(x => new { x.UserId, x.CategoryCode })
                .IsUnique();

            builder.Property(x => x.RowVersion)
                .IsRowVersion();
        }
    }
}
