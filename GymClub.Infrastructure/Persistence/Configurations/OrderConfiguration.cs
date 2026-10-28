using GymClub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymClub.Infrastructure.Persistence.Configurations;

public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("Carts");
        builder.HasIndex(c => c.UserId).IsUnique();
        builder.HasOne(c => c.User).WithOne()
            .HasForeignKey<Cart>(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(c => c.Items).WithOne(i => i.Cart)
            .HasForeignKey(i => i.CartId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItems");
        builder.Property(i => i.UnitPriceSnapshot).HasPrecision(18, 0);
        builder.HasIndex(i => new { i.CartId, i.ProductId }).IsUnique();
        builder.HasOne(i => i.Product).WithMany()
            .HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.Property(o => o.OrderNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(o => o.OrderNumber).IsUnique();
        builder.Property(o => o.Status).HasConversion<int>();
        builder.Property(o => o.SubTotal).HasPrecision(18, 0);
        builder.Property(o => o.DiscountAmount).HasPrecision(18, 0);
        builder.Property(o => o.ShippingCost).HasPrecision(18, 0);
        builder.Property(o => o.Total).HasPrecision(18, 0);
        builder.Property(o => o.ReceiverName).HasMaxLength(150).IsRequired();
        builder.Property(o => o.ReceiverPhone).HasMaxLength(20).IsRequired();
        builder.Property(o => o.Province).HasMaxLength(100).IsRequired();
        builder.Property(o => o.City).HasMaxLength(100).IsRequired();
        builder.Property(o => o.PostalCode).HasMaxLength(20);
        builder.Property(o => o.FullAddress).HasMaxLength(1000).IsRequired();
        builder.Property(o => o.TrackingCode).HasMaxLength(100);
        builder.Property(o => o.CancelReason).HasMaxLength(1000);

        builder.HasOne(o => o.User).WithMany()
            .HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.DiscountCode).WithMany(d => d.Orders)
            .HasForeignKey(o => o.DiscountCodeId).OnDelete(DeleteBehavior.SetNull);
        builder.HasMany(o => o.Items).WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(o => o.Transactions).WithOne(t => t.Order)
            .HasForeignKey(t => t.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");
        builder.Property(i => i.ProductNameSnapshot).HasMaxLength(200).IsRequired();
        builder.Property(i => i.ProductImageSnapshot).HasMaxLength(500);
        builder.Property(i => i.UnitPriceSnapshot).HasPrecision(18, 0);
        builder.Property(i => i.LineTotal).HasPrecision(18, 0);
        builder.HasOne(i => i.Product).WithMany()
            .HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class DiscountCodeConfiguration : IEntityTypeConfiguration<DiscountCode>
{
    public void Configure(EntityTypeBuilder<DiscountCode> builder)
    {
        builder.ToTable("DiscountCodes");
        builder.Property(d => d.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(d => d.Code).IsUnique();
        builder.Property(d => d.Type).HasConversion<int>();
        builder.Property(d => d.Value).HasPrecision(18, 0);
        builder.Property(d => d.MaxDiscountAmount).HasPrecision(18, 0);
        builder.Property(d => d.MinOrderAmount).HasPrecision(18, 0);
    }
}

public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("PaymentTransactions");
        builder.Property(t => t.Authority).HasMaxLength(100).IsRequired();
        builder.Property(t => t.RefId).HasMaxLength(100);
        builder.Property(t => t.Gateway).HasMaxLength(50);
        builder.Property(t => t.GatewayMessage).HasMaxLength(1000);
        builder.Property(t => t.Amount).HasPrecision(18, 0);
        builder.Property(t => t.Status).HasConversion<int>();
        builder.HasIndex(t => t.Authority);
    }
}