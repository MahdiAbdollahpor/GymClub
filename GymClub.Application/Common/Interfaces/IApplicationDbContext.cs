using GymClub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<OtpCode> OtpCodes { get; }
    DbSet<Article> Articles { get; }
    DbSet<ArticleCategory> ArticleCategories { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductCategory> ProductCategories { get; }
    DbSet<ProductImage> ProductImages { get; }
    DbSet<ProductAttribute> ProductAttributes { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}