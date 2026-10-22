using GymClub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace GymClub.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}