using GymClub.Domain.Common;
using GymClub.Domain.Enums;

namespace GymClub.Domain.Entities;

public class WorkoutPaymentTransaction : BaseEntity
{
    public Guid WorkoutRequestId { get; set; }
    public string Authority { get; set; } = string.Empty;
    public string? RefId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Initiated;
    public string Gateway { get; set; } = "ZarinPal";
    public DateTime? VerifiedAt { get; set; }
    public string? GatewayMessage { get; set; }

    public WorkoutRequest WorkoutRequest { get; set; } = null!;
}