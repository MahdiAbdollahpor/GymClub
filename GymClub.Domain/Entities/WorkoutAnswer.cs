using GymClub.Domain.Common;

namespace GymClub.Domain.Entities;

public class WorkoutAnswer : BaseEntity
{
    public Guid RequestId { get; set; }
    public Guid QuestionId { get; set; }

    public string? AnswerText { get; set; }
    public decimal? AnswerNumber { get; set; }
    public bool? AnswerBool { get; set; }
    public string? AnswerJson { get; set; }        // MultiChoice

    public WorkoutRequest Request { get; set; } = null!;
    public WorkoutQuestion Question { get; set; } = null!;
}