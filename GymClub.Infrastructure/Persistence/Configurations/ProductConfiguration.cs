using GymClub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymClub.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Slug).HasMaxLength(200).IsRequired();
        builder.HasIndex(p => p.Slug).IsUnique();
        builder.Property(p => p.Price).HasPrecision(18, 0);
        builder.Property(p => p.DiscountPrice).HasPrecision(18, 0);
        builder.Property(p => p.Description).HasMaxLength(4000);

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Images)
            .WithOne(i => i.Product)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Attributes)
            .WithOne(a => a.Product)
            .HasForeignKey(a => a.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("ProductCategories");
        builder.Property(c => c.Title).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Slug).HasMaxLength(150).IsRequired();
        builder.HasIndex(c => c.Slug).IsUnique();

        builder.HasOne(c => c.Parent)
            .WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages");
        builder.Property(i => i.Url).HasMaxLength(500).IsRequired();
    }
}

public class ProductAttributeConfiguration : IEntityTypeConfiguration<ProductAttribute>
{
    public void Configure(EntityTypeBuilder<ProductAttribute> builder)
    {
        builder.ToTable("ProductAttributes");
        builder.Property(a => a.Title).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Value).HasMaxLength(500).IsRequired();
    }
}