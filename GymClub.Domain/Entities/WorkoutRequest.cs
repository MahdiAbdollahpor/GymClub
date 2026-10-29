using GymClub.Domain.Common;
using GymClub.Domain.Enums;

namespace GymClub.Domain.Entities;

public class WorkoutRequest : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid QuestionnaireId { get; set; }
    public WorkoutRequestStatus Status { get; set; } = WorkoutRequestStatus.PendingPayment;

    public decimal Amount { get; set; }
    public Guid? PaymentTransactionId { get; set; }

    public DateTime? PaidAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? AdminNote { get; set; }

    public User User { get; set; } = null!;
    public WorkoutQuestionnaire Questionnaire { get; set; } = null!;
    public ICollection<WorkoutAnswer> Answers { get; set; } = new List<WorkoutAnswer>();
    public WorkoutProgram? Program { get; set; }
}