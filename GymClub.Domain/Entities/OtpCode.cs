using GymClub.Domain.Common;
using GymClub.Domain.Enums;

namespace GymClub.Domain.Entities;

public class OtpCode : BaseEntity
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;   // SHA256
    public OtpPurpose Purpose { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? UsedAt { get; set; }
}