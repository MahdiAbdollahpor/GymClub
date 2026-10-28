using GymClub.Domain.Common;

namespace GymClub.Domain.Entities;

public class Address : BaseEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;         // خانه / محل کار
    public string Province { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public string ReceiverPhone { get; set; } = string.Empty;
    public bool IsDefault { get; set; }

    public User User { get; set; } = null!;
}