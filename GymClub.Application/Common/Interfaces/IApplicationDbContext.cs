using GymClub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymClub.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<OtpCode> OtpCodes { get; }
    DbSet<Article> Articles { get; }
    DbSet<ArticleCategory> ArticleCategories { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}