using GymClub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymClub.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.FullName).HasMaxLength(150).IsRequired();
        builder.Property(u => u.DisplayUserName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Role).HasConversion<int>();
        builder.HasIndex(u => u.PhoneNumber).IsUnique();
    }
}