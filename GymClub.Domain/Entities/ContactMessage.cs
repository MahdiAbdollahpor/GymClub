using GymClub.Domain.Common;

namespace GymClub.Domain.Entities;

public class ContactMessage : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public Guid? RepliedByUserId { get; set; }
    public string? AdminReply { get; set; }
    public DateTime? RepliedAt { get; set; }
}