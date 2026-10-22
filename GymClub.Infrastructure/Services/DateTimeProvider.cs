using GymClub.Application.Common.Interfaces;

namespace GymClub.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}