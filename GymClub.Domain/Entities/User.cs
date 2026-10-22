using GymClub.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace GymClub.Domain.Entities;

public class User : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public string DisplayUserName { get; set; } = string.Empty;
    public bool IsPhoneConfirmed { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}