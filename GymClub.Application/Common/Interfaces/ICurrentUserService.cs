namespace GymClub.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? PhoneNumber { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}